using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TORSEPAN.Application.Interfaces;
namespace TORSEPAN.Infrastructure.Services;
public sealed record TelegramAlertSnapshot(DateTimeOffset? LastAttemptUtc, DateTimeOffset? LastSuccessUtc, string State, string? Error);

public sealed class TelegramAlertStatus
{
    private readonly object gate = new();
    private DateTimeOffset? lastAttemptUtc, lastSuccessUtc;
    private string state = "not-started";
    private string? error;

    public TelegramAlertSnapshot Snapshot()
    {
        lock (gate) return new(lastAttemptUtc, lastSuccessUtc, state, error);
    }

    public void Attempt() { lock (gate) { lastAttemptUtc = DateTimeOffset.UtcNow; state = "sending"; error = null; } }
    public void Success() { lock (gate) { lastSuccessUtc = DateTimeOffset.UtcNow; state = "succeeded"; error = null; } }
    public void Failure(string reason) { lock (gate) { state = "failed"; error = reason; } }
}

public sealed class TelegramInventoryAlertService(HttpClient http, IConfiguration configuration,
    TelegramAlertStatus status, ILogger<TelegramInventoryAlertService> logger) : IInventoryAlertService
{
    public async Task SendLowStockAsync(string itemName, string stockType, int quantity, int threshold, CancellationToken cancellationToken)
    {
        status.Attempt();
        var token = configuration["Telegram:BotToken"]; var chatId = configuration["Telegram:ChatId"];
        var text = $"⚠️ هشدار موجودی انبار مواد اولیه\n{itemName} - {stockType}\nموجودی فعلی: {quantity}\nحد هشدار: {threshold}";
        try
        {
            var relayUrl = configuration["Telegram:RelayUrl"];
            if (string.IsNullOrWhiteSpace(relayUrl))
                relayUrl = configuration["Telegram:BackupRelayUrl"]?
                    .Replace("telegram-database-backup", "telegram-inventory-alert", StringComparison.OrdinalIgnoreCase)
                    .Replace("telegram-payroll-report", "telegram-inventory-alert", StringComparison.OrdinalIgnoreCase)
                    .Replace("/database-backup", "/inventory-alert", StringComparison.OrdinalIgnoreCase)
                    .Replace("/payroll-report", "/inventory-alert", StringComparison.OrdinalIgnoreCase);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(25));
            using HttpResponseMessage response = await SendAsync(relayUrl, token, chatId, itemName, stockType,
                quantity, threshold, text, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                var reason = $"Telegram alert relay returned HTTP {(int)response.StatusCode}.";
                status.Failure(reason);
                logger.LogWarning("Low stock Telegram alert for {ItemName} failed: {Reason}", itemName, reason);
                return;
            }
            status.Success();
            logger.LogInformation("Low stock Telegram alert sent for {ItemName}.", itemName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status.Failure("Inventory alert request was cancelled.");
        }
        catch (Exception exception)
        {
            // HttpRequestException can contain the direct Telegram URL, including its bot token.
            var reason = exception is OperationCanceledException ? "Telegram alert timed out."
                : $"Telegram alert failed: {exception.GetType().Name}.";
            status.Failure(reason);
            logger.LogWarning("Low stock Telegram alert for {ItemName} failed: {Reason}", itemName, reason);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string? relayUrl, string? token, string? chatId,
        string itemName, string stockType, int quantity, int threshold, string text, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(relayUrl))
        {
            var secret = configuration["Telegram:RelaySecret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Telegram relay secret is not configured.");
            using var request = new HttpRequestMessage(HttpMethod.Post, relayUrl)
            {
                Content = Utf8Json(new { itemName, stockType, quantity, threshold })
            };
            request.Headers.Add("X-Relay-Secret", secret);
            return await http.SendAsync(request, ct);
        }
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
            throw new InvalidOperationException("Telegram bot or chat is not configured.");
        using var content = Utf8Json(new { chat_id = chatId, text });
        return await http.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", content, ct);
    }

    private static StringContent Utf8Json<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
