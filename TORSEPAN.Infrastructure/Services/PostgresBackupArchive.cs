using System.Diagnostics;
using Npgsql;

namespace TORSEPAN.Infrastructure.Services;

public static class PostgresBackupArchive
{
    public static async Task DumpAsync(NpgsqlConnectionStringBuilder connection, string file, string[] options, CancellationToken ct)
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

    public static NpgsqlConnectionStringBuilder BuildConnection(string configured)
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
