using System.Globalization;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public interface IOrderReminderSender
{
    Task SendAsync(string deliveryKey, string text, CancellationToken ct);
}

public sealed class TelegramOrderReminderSender(HttpClient http, IConfiguration config) : IOrderReminderSender
{
    public async Task SendAsync(string deliveryKey, string text, CancellationToken ct)
    {
        var relay = config["Telegram:OrderRelayUrl"];
        if (string.IsNullOrWhiteSpace(relay))
        {
            var existing = config["Telegram:RelayUrl"] ?? config["Telegram:BackupRelayUrl"];
            if (!string.IsNullOrWhiteSpace(existing))
            {
                var uri = new UriBuilder(existing);
                uri.Path = uri.Path[..(uri.Path.LastIndexOf('/') + 1)] +
                    (uri.Path.Contains("/api/internal/", StringComparison.Ordinal) ? "telegram-order-reminder" : "order-reminder");
                relay = uri.Uri.AbsoluteUri;
            }
        }
        if (string.IsNullOrWhiteSpace(relay)) throw new InvalidOperationException("Order reminder relay is not configured.");
        var secret = config["Telegram:RelaySecret"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Telegram relay secret is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, relay)
        { Content = JsonContent.Create(new { deliveryKey, text }) };
        request.Headers.Add("X-Relay-Secret", secret);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        using var response = await http.SendAsync(request, deadline.Token);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Order reminder relay returned HTTP {(int)response.StatusCode}.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
        if (!json.RootElement.TryGetProperty("status", out var status) || status.GetString() is not ("sent" or "already-sent"))
            throw new InvalidOperationException("Order reminder relay did not confirm delivery.");
    }
}

public sealed record OrderReminderSnapshot(DateTimeOffset? LastCheckUtc, string State, int Pending, string? Error);
public sealed class OrderReminderStatus
{
    private readonly object gate = new();
    private OrderReminderSnapshot current = new(null, "not-started", 0, null);
    public OrderReminderSnapshot Snapshot() { lock (gate) return current; }
    public void Update(DateTimeOffset time, string state, int pending, string? error = null)
    { lock (gate) current = new(time, state, pending, error); }
}

// Ledger entries are committed with the order. Deployments/restarts do not reset milestone delivery.
public sealed class OrderReminderProcessor(TORSEPANDbContext db, IOrderReminderSender sender, TimeProvider clock,
    OrderReminderStatus status, ILogger<OrderReminderProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Covers overlapping instances during rolling deployments. Released even after a crash.
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_try_advisory_xact_lock(730031, 1)";
        if (!Equals(await command.ExecuteScalarAsync(ct), true)) return;
        await DispatchDueAsync(ct);
        await transaction.CommitAsync(ct);
    }

    // Called under ProcessAsync's database lock in production; also allows deterministic delivery tests.
    public async Task DispatchDueAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var due = await db.OrderReminders.Include(x => x.Order)
            .Where(x => x.SentAtUtc == null && x.DueAtUtc <= now && x.NextAttemptAtUtc <= now)
            .OrderBy(x => x.DueAtUtc).ThenBy(x => x.Milestone).Take(20).ToListAsync(ct);
        var failed = false;
        foreach (var reminder in due)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await sender.SendAsync(reminder.DeliveryKey, BuildMessage(reminder.Order, reminder.Milestone, now), ct);
                reminder.Delivered(clock.GetUtcNow().UtcDateTime);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Exception URLs may contain secrets. Record only safe, bounded diagnostics.
                var reason = ex is InvalidOperationException ? ex.Message : $"Order reminder delivery failed: {ex.GetType().Name}.";
                reminder.Failed(now, reason);
                logger.LogWarning("Order {OrderId} milestone {Milestone} delivery failed: {Reason}", reminder.OrderId, reminder.Milestone, reason);
                failed = true;
            }
        }
        await db.SaveChangesAsync(ct);
        var pending = await db.OrderReminders.CountAsync(x => x.SentAtUtc == null && x.DueAtUtc <= now, ct);
        status.Update(clock.GetUtcNow(), failed ? "retrying" : "healthy", pending,
            failed ? "One or more reminders are awaiting a retry." : null);
    }

    public static string BuildMessage(CustomerOrder order, int milestone, DateTime now)
    {
        var title = milestone == 4 ? "⏰ پایان مهلت سفارش" : $"🔔 یادآوری سفارش — {milestone * 25}٪ زمان";
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        var elapsed = OrderTiming.ElapsedDays(order.CreatedAtUtc, now).ToString("0.##", culture);
        var remaining = OrderTiming.RemainingDays(order.DueAtUtc, now).ToString("0.##", culture);
        var due = TimeZoneInfo.ConvertTimeFromUtc(order.DueAtUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"))
            .ToString("yyyy/MM/dd HH:mm", culture);
        return $"{title}\nسفارش‌دهنده: {order.CustomerName}\nاسکیل: {order.ScaleName}\n" +
            $"مدت سفارش: {order.DurationDays} روز\nزمان گذشته: {elapsed} روز\nزمان باقی‌مانده: {remaining} روز\n" +
            $"موعد تحویل: {due} (تهران)" +
            (order.InstrumentCode is null ? "" : $"\nکد ساز: {order.InstrumentCode}") +
            (milestone == 4 ? "\nمهلت این سفارش به پایان رسیده است." : "");
    }
}

public sealed class OrderReminderService(IServiceScopeFactory scopes, TimeProvider clock,
    OrderReminderStatus status, ILogger<OrderReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OrderReminderProcessor>().ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                status.Update(clock.GetUtcNow(), "failed", 0, $"Reminder check failed: {ex.GetType().Name}.");
                logger.LogWarning("Order reminder check failed: {FailureType}", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromMinutes(1), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
