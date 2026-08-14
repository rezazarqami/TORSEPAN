using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TORSEPAN.Infrastructure.Services;

public sealed class NightlyDatabaseBackupService(IConfiguration config, IHttpClientFactory clients, ILogger<NightlyDatabaseBackupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Run once when a fresh container starts. Besides providing an immediate
        // safety copy, this makes deployment/configuration failures visible now
        // instead of waiting until the next night.
        try { await BackupAsync(ct); }
        catch (Exception ex) { logger.LogError(ex, "Initial database backup failed."); }

        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5));
            await Task.Delay(now.Date.AddDays(1).AddHours(2) - now, ct);
            try { await BackupAsync(ct); } catch (Exception ex) { logger.LogError(ex, "Nightly database backup failed."); }
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
            var info = new ProcessStartInfo("pg_dump") { RedirectStandardError=true, UseShellExecute=false };
            info.ArgumentList.Add("--format=custom"); info.ArgumentList.Add($"--file={file}"); info.ArgumentList.Add(db);
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
}
