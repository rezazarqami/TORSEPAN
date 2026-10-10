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
                Check(request.Version == HttpVersion.Version11 && request.VersionPolicy == HttpVersionPolicy.RequestVersionExact,
                    "backup relay upload uses HTTP/1.1 through the proxy");
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
            Check(status.ArchiveBytes > 0 && status.PartCount == 1, "health records the archive size and delivery part count");
            var retries = 0;
            using var retryHandler = new Handler(async (request, ct) =>
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                Check(body.Contains("PGDMP-test-data-only"), "retry reopens the whole archive stream");
                if (++retries == 1) throw new HttpRequestException("Error while copying content to a stream.");
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var retryStatus = new DatabaseBackupStatus();
            using var retry = new NightlyDatabaseBackupService(config, new Factory(retryHandler), retryStatus,
                NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(await Run(retry) && retries == 2 && retryStatus.State == "succeeded", "transient upload failure is retried and confirmed");
            var rejected = 0;
            using var rejectedHandler = new Handler((_, _) => { rejected++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)); });
            using var rejection = new NightlyDatabaseBackupService(config, new Factory(rejectedHandler), new DatabaseBackupStatus(), NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(!await Run(rejection) && rejected == 1, "invalid relay secret is not retried as a transport outage");
            using var failedHandler = new Handler((_, _) => throw new HttpRequestException("Error while copying content to a stream.",
                new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionReset)));
            var failedStatus = new DatabaseBackupStatus();
            using var failed = new NightlyDatabaseBackupService(config, new Factory(failedHandler), failedStatus,
                NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(!await Run(failed) && failedStatus.State == "failed" && failedStatus.Stage == "telegram-delivery" &&
                failedStatus.LastSuccessUtc is null, "SSL failure records delivery stage without false success");
            Check(failedStatus.TransportError!.Contains("ConnectionReset"), "inner transport failure is available without private URLs or credentials");
            await File.AppendAllTextAsync(dump, "truncate -s 9000000 \"$destination\"\n");
            var sizes = new List<int>();
            using var splitHandler = new Handler(async (request, ct) => {
                Check(request.Content!.Headers.ContentLength is > 0, "multipart backup has a known total Content-Length");
                sizes.Add((await request.Content.ReadAsByteArrayAsync(ct)).Length);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent("{\"status\":\"sent\"}") };
            });
            var splitStatus = new DatabaseBackupStatus();
            using var split = new NightlyDatabaseBackupService(config,new Factory(splitHandler),splitStatus,NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(await Run(split) && sizes.Count == 2 && sizes.All(x=>x<9_000_000) && splitStatus.PartCount==2,
                "large archives are split into bounded parts below the legacy panel limit");
            using var badReceiptHandler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent("<html>redirected page</html>") }));
            var receiptStatus = new DatabaseBackupStatus();
            using var badReceipt = new NightlyDatabaseBackupService(config,new Factory(badReceiptHandler),receiptStatus,NullLogger<NightlyDatabaseBackupService>.Instance);
            Check(!await Run(badReceipt) && receiptStatus.LastSuccessUtc is null,"a successful HTTP page without a delivery receipt is not a confirmed backup");
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
