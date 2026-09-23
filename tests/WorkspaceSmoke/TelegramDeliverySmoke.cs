using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TORSEPAN.Infrastructure.Services;

internal static class TelegramDeliverySmoke
{
    public static async Task RunAsync()
    {
        var received = new TaskCompletionSource<HttpRequestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new Handler(async (request, _) =>
        {
            received.SetResult(request);
            return await release.Task;
        }));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:BackupRelayUrl"] = "https://example.test/api/internal/telegram-database-backup",
            ["Telegram:RelaySecret"] = "test-secret"
        }).Build();
        var status = new TelegramAlertStatus();
        var alerts = new TelegramInventoryAlertService(client, config, status,
            NullLogger<TelegramInventoryAlertService>.Instance);

        var sending = alerts.SendLowStockAsync("steel", "stock", 2, 5, CancellationToken.None);
        var request = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(!sending.IsCompleted, "low stock alert waits for relay response");
        Check(request.RequestUri?.AbsolutePath == "/api/internal/telegram-inventory-alert" &&
            request.Headers.GetValues("X-Relay-Secret").Single() == "test-secret",
            "backup-only relay config routes inventory alerts to the inventory endpoint");
        release.SetResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        await sending;
        Check(status.Snapshot().State == "failed" && status.Snapshot().Error?.Contains("401") == true,
            "relay rejection is visible in alert health status");

        var successStatus = new TelegramAlertStatus();
        using var successful = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var successfulAlerts = new TelegramInventoryAlertService(successful, config, successStatus,
            NullLogger<TelegramInventoryAlertService>.Instance);
        await successfulAlerts.SendLowStockAsync("steel", "stock", 1, 5, CancellationToken.None);
        Check(successStatus.Snapshot().State == "succeeded" && successStatus.Snapshot().LastSuccessUtc.HasValue,
            "a confirmed relay response records alert delivery");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
