using System.Diagnostics;
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
}

public sealed class NightlyDatabaseBackupService(IConfiguration config, IHttpClientFactory clients,
    DatabaseBackupStatus status, ILogger<NightlyDatabaseBackupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Run once when a fresh container starts. Besides providing an immediate
        // safety copy, this makes deployment/configuration failures visible now
        // instead of waiting until the next night.
        await TryBackupAsync("Initial", ct);

        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5));
            await Task.Delay(now.Date.AddDays(1).AddHours(2) - now, ct);
            await TryBackupAsync("Nightly", ct);
        }
    }

    private async Task TryBackupAsync(string runType, CancellationToken ct)
    {
        status.LastAttemptUtc = DateTimeOffset.UtcNow;
        status.State = "running";
        status.Error = null;
        try
        {
            await BackupAsync(ct);
            status.LastSuccessUtc = DateTimeOffset.UtcNow;
            status.State = "succeeded";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            status.State = "failed";
            status.Error = $"{ex.GetType().Name}: {ex.Message}";
            logger.LogError(ex, "{RunType} database backup failed.", runType);
        }
    }

    private async Task BackupAsync(CancellationToken ct)
    {
        var db = config["DATABASE_URL"] ?? config.GetConnectionString("DefaultConnection");
        var relay = config["Telegram:RelayUrl"];
        if (string.IsNullOrWhiteSpace(db))
            throw new InvalidOperationException("Database backup connection is not configured.");
        if (string.IsNullOrWhiteSpace(relay))
            throw new InvalidOperationException("Telegram backup relay is not configured.");
        var url = relay.Replace("telegram-inventory-alert", "telegram-database-backup", StringComparison.OrdinalIgnoreCase);
        var tehranNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5));
        var file = Path.Combine(Path.GetTempPath(), $"TORSEPAN-{tehranNow:yyyy-MM-dd-HHmm}.dump");
        try
        {
            var connection = BuildConnection(db);
            var info = new ProcessStartInfo("pg_dump") { RedirectStandardError=true, UseShellExecute=false };
            info.ArgumentList.Add("--format=custom");
            info.ArgumentList.Add($"--file={file}");
            info.ArgumentList.Add($"--host={connection.Host}");
            info.ArgumentList.Add($"--port={connection.Port}");
            info.ArgumentList.Add($"--username={connection.Username}");
            info.ArgumentList.Add($"--dbname={connection.Database}");
            info.Environment["PGPASSWORD"] = connection.Password;
            info.Environment["PGSSLMODE"] = connection.SslMode == SslMode.Disable ? "disable" : "require";
            var process = Process.Start(info) ?? throw new InvalidOperationException("pg_dump failed to start.");
            await process.WaitForExitAsync(ct);
            if(process.ExitCode!=0) throw new InvalidOperationException(await process.StandardError.ReadToEndAsync(ct));
            using var form=new MultipartFormDataContent(); await using var stream=File.OpenRead(file);
            form.Add(new StreamContent(stream),"backup",Path.GetFileName(file));
            using var request=new HttpRequestMessage(HttpMethod.Post,url){Content=form}; request.Headers.Add("X-Relay-Secret",config["Telegram:RelaySecret"]);
            using var response=await clients.CreateClient().SendAsync(request,ct); response.EnsureSuccessStatusCode();
            logger.LogInformation("Database backup sent successfully at {BackupTime} Tehran time.", tehranNow);
        }
        finally { if(File.Exists(file)) File.Delete(file); }
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
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = credentials[0],
            Password = credentials[1],
            SslMode = SslMode.Require
        };
    }
}
