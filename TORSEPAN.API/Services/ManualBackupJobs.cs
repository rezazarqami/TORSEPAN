using System.Security.Cryptography;
using System.Threading.Channels;
using TORSEPAN.Infrastructure.Services;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.API.Reporting;

namespace TORSEPAN.API.Services;

// Jobs keep generation outside the HTTP/proxy timeout. Files never enter the Blazor circuit.
public sealed class ManualBackupJobs(IServiceScopeFactory scopes, IConfiguration configuration,
    ILogger<ManualBackupJobs> logger) : BackgroundService
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, Job> jobs = [];
    private readonly Channel<Job> queue = Channel.CreateUnbounded<Job>(new() { SingleReader = true });
    private readonly string root = Path.Combine(Path.GetTempPath(), "torsepan-manual-backups");
    public BackupJobView? Create(Guid owner, string kind)
    {
        lock (gate)
        {
            var pending = jobs.Values.FirstOrDefault(x => x.Owner == owner && x.State is "queued" or "running");
            if (pending is not null) return View(pending);
            if (jobs.Values.Count(x => x.State is "queued" or "running") >= 2) return null;
            var job = new Job { Id = Guid.NewGuid(), Owner = owner, Kind = kind };
            jobs.Add(job.Id, job);
            queue.Writer.TryWrite(job);
            return View(job);
        }
    }
    public BackupJobView? Get(Guid id, Guid owner)
    {
        lock (gate) return jobs.TryGetValue(id, out var job) && job.Owner == owner ? View(job) : null;
    }
    public string? Ticket(Guid id, Guid owner)
    {
        lock (gate)
        {
            if (!jobs.TryGetValue(id, out var job) || job.Owner != owner || job.State != "ready") return null;
            job.Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            job.TokenExpires = DateTimeOffset.UtcNow.AddMinutes(5);
            return job.Token;
        }
    }
    public BackupDownload? Download(string token)
    {
        lock (gate)
        {
            var job = jobs.Values.FirstOrDefault(x => x.Token == token && x.TokenExpires > DateTimeOffset.UtcNow && x.State == "ready");
            if (job?.Path is null || !File.Exists(job.Path)) return null;
            // Open while holding the cleanup lock. Existing downloads survive file expiry.
            return new(File.OpenRead(job.Path), job.FileName!, job.Kind == "pdf" ? "application/pdf" : "application/octet-stream");
        }
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using var cleanupCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var cleanup = CleanupLoopAsync(cleanupCancellation.Token);
        try
        {
            await foreach (var job in queue.Reader.ReadAllAsync(ct))
            {
                lock (gate) job.State = "running";
                var path = Path.Combine(root, job.Id.ToString("N"));
                try
                {
                    if (job.Kind == "full")
                    {
                        var connection = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Backup database is not configured.");
                        // No table exclusions: photos and thumbnails are in HandpanPhotos bytea columns.
                        await PostgresBackupArchive.DumpAsync(PostgresBackupArchive.BuildConnection(connection), path, [], ct);
                    }
                    else
                    {
                        using var scope = scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<TORSEPANDbContext>();
                        var snapshot = await PassportBackupSnapshot.LoadAsync(db, ct);
                        var pdf = PassportBackupPdf.Build(snapshot);
                        await File.WriteAllBytesAsync(path, pdf, ct);
                    }
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    lock (gate)
                    {
                        job.Path = path; job.Bytes = new FileInfo(path).Length;
                        job.FileName = $"TORSEPAN-{(job.Kind == "full" ? "FULL-WITH-PHOTOS" : "PASSPORTS")}-{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.5)):yyyyMMdd-HHmmss}.{(job.Kind == "full" ? "dump" : "pdf")}";
                        job.State = "ready"; job.Expires = DateTimeOffset.UtcNow.AddHours(1);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { DeleteTemporary(path); throw; }
                catch (Exception ex)
                {
                    DeleteTemporary(path);
                    // pg_dump diagnostics remain server-side; responses never contain connection secrets.
                    logger.LogError(ex, "Manual {Kind} backup {Id} failed", job.Kind, job.Id);
                    lock (gate) { job.State = "failed"; job.Error = "ساخت فایل انجام نشد؛ دوباره تلاش کنید یا لاگ API را بررسی کنید."; }
                }
            }
        }
        finally
        {
            cleanupCancellation.Cancel();
            try { await cleanup; } catch (OperationCanceledException) when (cleanupCancellation.IsCancellationRequested) { }
        }
    }
    private async Task CleanupLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            lock (gate)
            {
                foreach (var job in jobs.Values.Where(x => x.Expires <= DateTimeOffset.UtcNow && x.State is not ("queued" or "running")).ToArray())
                { if (job.Path is not null) DeleteTemporary(job.Path); jobs.Remove(job.Id); }
                // Files from a previous process are inaccessible and removed after their maximum retention.
                foreach (var file in Directory.EnumerateFiles(root))
                    if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddHours(-2)) DeleteTemporary(file);
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }
    private void DeleteTemporary(string path)
    {
        try { File.Delete(path); }
        catch (IOException ex) { logger.LogWarning(ex, "Temporary backup cleanup will retry later"); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Temporary backup cleanup lacks file access"); }
    }
    private static BackupJobView View(Job x) => new(x.Id, x.Kind, x.State, x.Bytes, x.FileName, x.Error, x.Expires);
    private sealed class Job
    {
        public Guid Id; public Guid Owner; public string Kind = ""; public string State = "queued";
        public string? Path; public string? FileName; public long? Bytes; public string? Error;
        public DateTimeOffset Expires = DateTimeOffset.UtcNow.AddHours(1);
        public string? Token; public DateTimeOffset TokenExpires;
    }
}
public sealed record BackupJobView(Guid Id, string Kind, string State, long? Bytes, string? FileName, string? Error, DateTimeOffset Expires);
public sealed record BackupDownload(Stream Stream, string FileName, string ContentType);
