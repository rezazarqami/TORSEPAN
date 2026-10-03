using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TORSEPAN.Infrastructure.Services;

internal static class DatabaseBackupSmoke
{
    public static async Task RunAsync()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "backup-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            var dump = Path.Combine(directory, "pg_dump");
            await File.WriteAllTextAsync(dump, "#!/bin/sh\nfor arg in \"$@\"; do\n case \"$arg\" in\n --file=*) destination=${arg#--file=} ;;\n --exclude-table-data=*) excluded=yes ;;\n --data-only|--table=*) exit 19 ;;\n esac\ndone\n[ \"$excluded\" = yes ] || exit 20\nprintf 'PGDMP-test-data-only' > \"$destination\"\n");
            File.SetUnixFileMode(dump, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Environment.SetEnvironmentVariable("PATH", directory + Path.PathSeparator + originalPath);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Telegram:BackupRelayUrl"] = "",
                ["Telegram:RelayUrl"] = "https://example.test/api/internal/telegram-inventory-alert",
                ["Telegram:RelaySecret"] = "test-secret"
            }).Build();
            var deliveries = 0;
            using var handler = new Handler(async (request, ct) =>
            {
                deliveries++;
                Check(request.RequestUri!.AbsolutePath == "/api/internal/telegram-database-backup", "backup uses fallback relay route");
                var body = await request.Content!.ReadAsStringAsync(ct);
                Check(body.Contains("TORSEPAN-DATA-") && !body.Contains("TORSEPAN-PHOTOS-"), "only business data archive is sent");
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var status = new DatabaseBackupStatus();
            using var service = new NightlyDatabaseBackupService(config, new Factory(handler), status,
                NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(await Run(service), "data-only backup completes without a photo dump");
            Check(deliveries == 1 && status.LastSuccessUtc.HasValue && status.Stage == "completed" && status.Mode == "data-only",
                "one archive is confirmed and recorded in health");
            using var failedHandler = new Handler((_, _) => throw new HttpRequestException("SSL connection failed"));
            var failedStatus = new DatabaseBackupStatus();
            using var failed = new NightlyDatabaseBackupService(config, new Factory(failedHandler), failedStatus,
                NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(!await Run(failed) && failedStatus.State == "failed" && failedStatus.Stage == "telegram-delivery" &&
                failedStatus.LastSuccessUtc is null, "SSL failure records delivery stage without false success");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(directory, true);
        }
    }

    private static Task<bool> Run(NightlyDatabaseBackupService service) =>
        (Task<bool>)typeof(NightlyDatabaseBackupService).GetMethod("TryBackupAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, ["Test", CancellationToken.None])!;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
}
