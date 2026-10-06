using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TORSEPAN.Infrastructure.Services;

public sealed class DatabaseBackupStatus
{
    public DateTimeOffset? LastAttemptUtc { get; internal set; }
    public DateTimeOffset? LastSuccessUtc { get; internal set; }
    public string State { get; internal set; } = "not-started";
    public string? Error { get; internal set; }
    public string Stage { get; internal set; } = "idle";
    public string Mode => "data-only";
}

public sealed class NightlyDatabaseBackupService(IConfiguration config, IHttpClientFactory clients,
    DatabaseBackupStatus status, ILogger<NightlyDatabaseBackupService> logger) : BackgroundService
{
    // Both the relay (49 MiB) and Telegram (50 MB) must accept each multipart request.
    private const int PartSize = 45 * 1024 * 1024;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var succeeded = await TryBackupAsync("Initial", ct);
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5));
            var next = new DateTimeOffset(now.Year, now.Month, now.Day, 2, 0, 0, now.Offset);
            if (next <= now) next = next.AddDays(1);
            // A missed backup must retry; otherwise an outage can silently skip a full day.
            var delay = succeeded ? next - now : TimeSpan.FromMinutes(30);
            await Task.Delay(delay, ct);
            succeeded = await TryBackupAsync(succeeded ? "Nightly" : "Retry", ct);
        }
    }

    private async Task<bool> TryBackupAsync(string runType, CancellationToken ct)
    {
        status.LastAttemptUtc = DateTimeOffset.UtcNow;
        status.State = "running";
        status.Error = null;
        try
        {
            await BackupAsync(ct);
            status.LastSuccessUtc = DateTimeOffset.UtcNow;
            status.State = "succeeded";
            status.Stage = "completed";
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            status.State = "failed";
            status.Error = $"{ex.GetType().Name}: {ex.Message}";
            logger.LogError(ex, "{RunType} database backup failed.", runType);
            return false;
        }
    }

    private async Task BackupAsync(CancellationToken ct)
    {
        status.Stage = "configuration";
        var db = config["DATABASE_URL"] ?? config.GetConnectionString("DefaultConnection");
        var relay = config["Telegram:BackupRelayUrl"];
        if (string.IsNullOrWhiteSpace(relay)) relay = config["Telegram:RelayUrl"];
        var secret = config["Telegram:RelaySecret"];
        if (string.IsNullOrWhiteSpace(db))
            throw new InvalidOperationException("Database backup connection is not configured.");
        if (string.IsNullOrWhiteSpace(relay) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Telegram backup relay or secret is not configured.");

        // A shared inventory relay URL may point to either the panel endpoint or the standalone relay.
        var url = relay.Replace("telegram-inventory-alert", "telegram-database-backup", StringComparison.OrdinalIgnoreCase)
            .Replace("/inventory-alert", "/database-backup", StringComparison.OrdinalIgnoreCase);
        var connection = BuildConnection(db);
        var stamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5)).ToString("yyyy-MM-dd-HHmm");
        var directory = Path.Combine(Path.GetTempPath(), $"torsepan-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var data = Path.Combine(directory, $"TORSEPAN-DATA-{stamp}.dump");
            // Keep the photo table schema, but never dump or send its binary rows.
            status.Stage = "database-dump";
            await DumpAsync(connection, data, "--exclude-table-data=public.\"HandpanPhotos\"", ct);
            using var client = clients.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            status.Stage = "telegram-delivery";
            await SendArchiveAsync(client, url, secret, data, ct);
            logger.LogInformation("Data-only database backup sent to Telegram at {BackupTime} Tehran time.", stamp);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static Task DumpAsync(NpgsqlConnectionStringBuilder connection, string file, string option, CancellationToken ct) =>
        DumpAsync(connection, file, [option], ct);

    private static async Task DumpAsync(NpgsqlConnectionStringBuilder connection, string file, string[] options, CancellationToken ct)
    {
        var info = new ProcessStartInfo("pg_dump") { RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add("--format=custom");
        foreach (var option in options) info.ArgumentList.Add(option);
        info.ArgumentList.Add($"--file={file}");
        info.ArgumentList.Add($"--host={connection.Host}");
        info.ArgumentList.Add($"--port={connection.Port}");
        info.ArgumentList.Add($"--username={connection.Username}");
        info.ArgumentList.Add($"--dbname={connection.Database}");
        info.Environment["PGPASSWORD"] = connection.Password;
        info.Environment["PGSSLMODE"] = connection.SslMode == SslMode.Disable ? "disable" : "require";
        info.Environment["PGCONNECT_TIMEOUT"] = "30";
        using var dumpTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        dumpTimeout.CancelAfter(TimeSpan.FromMinutes(10));
        using var process = Process.Start(info) ?? throw new InvalidOperationException("pg_dump failed to start.");
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        try { await process.WaitForExitAsync(dumpTimeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            if (!ct.IsCancellationRequested)
                throw new TimeoutException("Database dump exceeded the 10-minute limit.");
            throw;
        }
        var error = await errorTask;
        if (process.ExitCode != 0) throw new InvalidOperationException($"pg_dump failed (exit {process.ExitCode}): {error}");
        if (new FileInfo(file).Length == 0) throw new InvalidOperationException("pg_dump produced an empty archive.");
    }

    private static async Task SendArchiveAsync(HttpClient client, string url, string secret, string archive, CancellationToken ct)
    {
        await using var source = File.OpenRead(archive);
        var total = Math.Max(1, (int)((source.Length + PartSize - 1) / PartSize));
        var buffer = new byte[64 * 1024];
        for (var index = 1; index <= total; index++)
        {
            var part = Path.Combine(Path.GetDirectoryName(archive)!, $"part-{index:D4}");
            var remaining = Math.Min(PartSize, source.Length - source.Position);
            await using (var output = File.Create(part))
            {
                while (remaining > 0)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), ct);
                    if (read == 0) throw new EndOfStreamException("Backup archive ended during splitting.");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                    remaining -= read;
                }
            }
            try
            {
                var name = total == 1 ? Path.GetFileName(archive) : $"{Path.GetFileName(archive)}.part{index:D4}-of-{total:D4}";
                // Reopen the part for each attempt so retries never send an exhausted stream.
                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        using var form = new MultipartFormDataContent();
                        await using var stream = File.OpenRead(part);
                        var file = new StreamContent(stream);
                        file.Headers.ContentLength = stream.Length;
                        form.Add(file, "backup", name);
                        using var request = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = form,
                            Version = System.Net.HttpVersion.Version11,
                            VersionPolicy = HttpVersionPolicy.RequestVersionExact
                        };
                        request.Headers.ExpectContinue = false;
                        request.Headers.Add("X-Relay-Secret", secret);
                        using var response = await client.SendAsync(request, ct);
                        if (!response.IsSuccessStatusCode)
                        {
                            var reason = response.StatusCode switch
                            {
                                System.Net.HttpStatusCode.Unauthorized => "Relay secret was rejected (401).",
                                System.Net.HttpStatusCode.NotFound => "Backup relay route was not found (404).",
                                _ => await RelayFailureAsync(response, ct)
                            };
                            throw new HttpRequestException($"Telegram backup {name} failed: {reason}", null, response.StatusCode);
                        }
                        break;
                    }
                    catch (HttpRequestException ex) when (attempt < 3 &&
                        (ex.StatusCode is null || ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)ex.StatusCode >= 500))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
                    }
                }
            }
            finally { File.Delete(part); }
        }
    }

    private static async Task<string> RelayFailureAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (json.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                var message = detail.GetString();
                if (!string.IsNullOrWhiteSpace(message)) return message[..Math.Min(message.Length, 180)];
            }
        }
        catch (JsonException) { }
        return $"relay returned HTTP {(int)response.StatusCode}.";
    }

    private static NpgsqlConnectionStringBuilder BuildConnection(string configured)
    {
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri))
            return new NpgsqlConnectionStringBuilder(configured);
        var credentials = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        if (credentials.Length != 2)
            throw new InvalidOperationException("Backup database credentials are incomplete.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host, Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'), Username = credentials[0],
            Password = credentials[1], SslMode = SslMode.Require
        };
    }
}
