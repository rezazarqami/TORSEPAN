using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;

namespace TORSEPAN.API.Services;
public static class MessagePushValidation
{
    public static bool Endpoint(string? endpoint)
    {
        if (endpoint is null || endpoint.Length > 2048 || !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.Port != 443 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) return false;
        // Only browser push providers; a subscription must never become an arbitrary server-side HTTP request.
        return uri.Host == "fcm.googleapis.com" || uri.Host == "updates.push.services.mozilla.com" ||
            uri.Host == "web.push.apple.com" || uri.Host.EndsWith(".push.apple.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host == "wns2-db5p.notify.windows.com" || uri.Host.EndsWith(".notify.windows.com", StringComparison.OrdinalIgnoreCase);
    }
    public static bool Key(string? value, int bytes)
    {
        try
        {
            if (value is null || value.Length > 100) return false;
            var decoded = Decode(value); if (decoded.Length != bytes) return false;
            if (bytes == 65)
            {
                if (decoded[0] != 4) return false;
                using var key = ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = decoded[1..33], Y = decoded[33..65] } });
            }
            return true;
        }
        catch { return false; }
    }
    public static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '='));
    public static string Hash(string endpoint) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)));
}
public sealed class MessagePushKeyStore(TORSEPANDbContext db)
{
    public async Task<MessagePushKeys> GetAsync(CancellationToken ct)
    {
        var keys = await db.MessagePushKeys.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (keys is not null) return keys;
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);
        static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var generated = new MessagePushKeys { PublicKey = Encode([4, .. parameters.Q.X!, .. parameters.Q.Y!]), PrivateKey = Encode(parameters.D!) };
        // Persist once, including across restarts and multiple API replicas. Never return the private key to a client.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"MessagePushKeys\" (\"Id\", \"PublicKey\", \"PrivateKey\") VALUES (1, {generated.PublicKey}, {generated.PrivateKey}) ON CONFLICT (\"Id\") DO NOTHING", ct);
        return await db.MessagePushKeys.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
    }
}
public interface IMessagePushTransport
{
    Task SendAsync(MessagePushSubscription subscription, MessagePushKeys keys, string subject, string payload, CancellationToken ct);
}
public sealed class MessagePushTransportException(HttpStatusCode status) : Exception { public HttpStatusCode StatusCode { get; } = status; }
public sealed class WebMessagePushTransport(HttpClient http) : IMessagePushTransport
{
    public async Task SendAsync(MessagePushSubscription subscription, MessagePushKeys keys, string subject, string payload, CancellationToken ct)
    {
        var client = new PushServiceClient(http) { AutoRetryAfter = false };
        using var authentication = new VapidAuthentication(keys.PublicKey, keys.PrivateKey) { Subject = subject };
        try
        {
            await client.RequestPushMessageDeliveryAsync(new PushSubscription { Endpoint = subscription.Endpoint, Keys = new Dictionary<string, string> { ["p256dh"] = subscription.P256dh, ["auth"] = subscription.Auth } },
                new PushMessage(payload) { TimeToLive = 86400, Urgency = PushMessageUrgency.High }, authentication, ct);
        }
        catch (PushServiceClientException ex) { throw new MessagePushTransportException(ex.StatusCode); }
    }
}
public sealed class MessagePushWorker(IServiceScopeFactory scopes, ILogger<MessagePushWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogWarning("Message push dispatch delayed ({ErrorType})", ex.GetType().Name); }
            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) { break; }
        }
    }
    public async Task DispatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TORSEPANDbContext>();
        var now = DateTime.UtcNow;
        await db.MessagePushDeliveries.Where(x => x.ExpiresAt <= now || x.Attempts >= 8).ExecuteDeleteAsync(ct);
        var pending = await db.MessagePushDeliveries.AsNoTracking().Where(x => x.NextAttemptAt <= now).OrderBy(x => x.NextAttemptAt).Take(30).ToListAsync(ct);
        if (pending.Count == 0) return;
        var keys = await scope.ServiceProvider.GetRequiredService<MessagePushKeyStore>().GetAsync(ct);
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var subject = configuration["WebPush:Subject"] ?? "https://torsepan.liara.run";
        var transport = scope.ServiceProvider.GetRequiredService<IMessagePushTransport>();
        foreach (var delivery in pending)
        {
            // Lease with compare-and-update: concurrent API replicas cannot send the same pending row together.
            var claimed = await db.MessagePushDeliveries.Where(x => x.Id == delivery.Id && x.NextAttemptAt == delivery.NextAttemptAt)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddMinutes(2)).SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
            if (claimed == 0) continue;
            var subscription = await db.MessagePushSubscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == delivery.SubscriptionId, ct);
            var valid = subscription is not null && subscription.UserId == delivery.UserId && subscription.CredentialVersion == delivery.CredentialVersion &&
                await db.Users.AnyAsync(x => x.Id == delivery.UserId && x.IsActive && !x.IsDeleted && x.CredentialVersion == delivery.CredentialVersion, ct);
            if (!valid) { await RemoveDelivery(); continue; }
            try
            {
                var totalIncoming = await db.WorkshopMessageReceipts.LongCountAsync(x => x.RecipientId == delivery.UserId && x.Message.SenderId != delivery.UserId, ct);
                var payload = JsonSerializer.Serialize(new { title = "تورسپن · پیام جدید", body = "پیام جدیدی در کارگاه دارید. برای مشاهده لمس کنید.", tag = $"message-{delivery.MessageId:N}", url = "/notifications", userId = delivery.UserId, totalIncoming });
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(15));
                await transport.SendAsync(subscription!, keys, subject, payload, timeout.Token);
                await RemoveDelivery();
            }
            catch (MessagePushTransportException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            { await db.MessagePushSubscriptions.Where(x => x.Id == delivery.SubscriptionId).ExecuteDeleteAsync(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                log.LogWarning("Message push attempt {Attempt} failed ({ErrorType})", delivery.Attempts + 1, ex.GetType().Name);
                await db.MessagePushDeliveries.Where(x => x.Id == delivery.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(Math.Min(3600, 30 * Math.Pow(2, delivery.Attempts)))), ct);
            }
            Task<int> RemoveDelivery() => db.MessagePushDeliveries.Where(x => x.Id == delivery.Id).ExecuteDeleteAsync(ct);
        }
    }
}
