using System.Reflection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Reporting;
using TORSEPAN.API.Services;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

internal static class ManualBackupSmoke
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public static async Task RunAsync()
    {
        Check(typeof(BackupsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Administrator", "full backup requires administrator");
        Check(typeof(BackupsController).GetMethod("Download")!.GetCustomAttribute<AllowAnonymousAttribute>() is not null, "download uses a short-lived capability, not JWT in URL");
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var user = new User("backup-test", "شاهین نجفی"); user.SetRole("Administrator");
        var helper = new User("backup-helper", "محمدرضا رنجبر");
        var material = new Material("استیل", quantity: 17); var scale = new Scale("D Kurd 14", ScaleUsage.All);
        db.AddRange(user, helper, material, scale); await db.SaveChangesAsync();
        var statuses = new[] { ProductionStage.WaitingForTune, ProductionStage.WaitingForQualityControl, ProductionStage.WaitingForPackaging, ProductionStage.FinishedWarehouse, ProductionStage.Sold, ProductionStage.Rejected };
        var created = new List<Handpan>();
        for (var i = 0; i < statuses.Length; i++)
        {
            var top = new Bowl($"TOP-{i}", BowlType.Top, true, InstrumentType.Standard, material.Id); top.SetScale(scale.Id);
            var bottom = new Bowl($"BOTTOM-{i}", BowlType.Bottom, false, InstrumentType.Standard, material.Id);
            var assembly = new HandpanAssembly(top.Id, bottom.Id); var handpan = assembly.CreateHandpan((855 + i).ToString());
            handpan.SetScale(scale.Id); handpan.ChangeStage(statuses[i]);
            if (statuses[i] == ProductionStage.Sold) { handpan.ChangeStage(ProductionStage.FinishedWarehouse); handpan.Sell("مشتری نمونه", 100000, null, user.Id); }
            var operation = new ProductionEvent(handpan.Id, assembly.Id, top.Id, user.Id, ProductionAction.Shape, EventResult.Completed, OperationDuration.Minutes20, $"Shape completed|CONTRIB:Stretch={helper.Id}");
            db.AddRange(top, bottom, assembly, handpan, operation);
            db.Add(new ProductionEvent(null, null, bottom.Id, helper.Id, ProductionAction.Tune, EventResult.Completed, null, "Tune completed"));
            db.Add(new ProductionEvent(null, null, top.Id, user.Id, ProductionAction.Design, EventResult.Completed, null, "DESIGN:id:اسیدی"));
            db.Add(new HandpanPhoto(handpan.Id, [1,2,3], [4,5], "image/jpeg", user.Id));
            created.Add(handpan);
        }
        var loose = new Bowl("SS-00090", BowlType.Top, true, InstrumentType.Standard, material.Id);
        db.Add(loose); db.Add(new ProductionEvent(null, null, loose.Id, user.Id, ProductionAction.Dimple, EventResult.Completed, null, "Dimple completed"));
        await db.SaveChangesAsync();
        var snapshot = await PassportBackupSnapshot.LoadAsync(db, default);
        Check(snapshot.Records.Count == 7 && snapshot.Records.Count(x => x.Kind == "ساز") == 6, "every handpan and loose bowl is present without duplicating assembled bowls");
        Check(PassportBackupSnapshot.Groups.All(group => snapshot.Records.Any(x => x.Group == group)), "production QC packaging warehouse sold and rejected are separate");
        var first = snapshot.Records.Single(x => x.Code == "855");
        Check(first.Operations.Count == 3 && first.Operations.Select(x => x.Id).Distinct().Count() == 3, "event linked by handpan assembly and bowl appears exactly once");
        Check(first.Operations.Any(x => x.Bowl.StartsWith("کاسه زیر") && x.Actor == helper.FullName), "bottom bowl performer is retained");
        Check(first.Operations.Any(x => x.Description.Contains("کشش توسط محمدرضا رنجبر") && x.Duration == "20 دقیقه"), "contributions resolve to names and durations remain readable");
        Check(first.Design == "اسیدی" && first.Scale == "D Kurd 14", "passport design and scale are included");
        Check(snapshot.Records.Single(x => x.Group == "فروش").Details.Contains("مشتری نمونه") && snapshot.Materials.Single().Quantity == 17, "sales and current stock included");
        QuestPDF.Settings.License = LicenseType.Community;
        using var font = typeof(BackupsController).Assembly.GetManifestResourceStream("TORSEPAN.API.Assets.Vazirmatn-Regular.ttf")!;
        FontManager.RegisterFont(font);
        // A long passport and mixed Persian/Latin values must flow across pages without clipping.
        var longRecord = first with { Operations = Enumerable.Range(1, 95).Select(i => first.Operations[0] with { Description = $"توضیحات عملیات شماره {i}: بررسی کاسه رو و کشش نوت‌ها" }).ToArray() };
        var pdf = PassportBackupPdf.Build(snapshot with { Records = snapshot.Records.Concat([longRecord with { Code = "LONG-855" }]).ToArray() });
        Check(pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8), "multi-page archive PDF renders");
        var output = Environment.GetEnvironmentVariable("TORSEPAN_BACKUP_PREVIEW");
        if (!string.IsNullOrWhiteSpace(output)) await File.WriteAllBytesAsync(output, pdf);
        await CheckJobsAsync();
    }
    private static async Task CheckHttpAsync(ManualBackupJobs jobs)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(jobs);
        builder.Services.AddControllers().AddApplicationPart(typeof(BackupsController).Assembly);
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, BackupTestAuth>("test", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var unauthorized = await client.PostAsJsonAsync("api/backups", new { kind = "full" });
        Check(unauthorized.StatusCode == HttpStatusCode.Unauthorized, "unauthenticated creation is rejected by the HTTP pipeline");
        client.DefaultRequestHeaders.Add("X-Test-Owner", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Workshop");
        using var forbidden = await client.PostAsJsonAsync("api/backups", new { kind = "full" });
        Check(forbidden.StatusCode == HttpStatusCode.Forbidden, "non-administrator creation is forbidden");
        client.DefaultRequestHeaders.Remove("X-Test-Role"); client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        using var accepted = await client.PostAsJsonAsync("api/backups", new { kind = "full" });
        Check(accepted.StatusCode == HttpStatusCode.Accepted, "administrator creates a queued job over HTTP");
        var job = (await accepted.Content.ReadFromJsonAsync<BackupJobView>())!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while ((await client.GetFromJsonAsync<BackupJobView>($"api/backups/{job.Id}"))!.State is "running" or "queued") await Task.Delay(30, timeout.Token);
        using var result = await client.PostAsJsonAsync($"api/backups/{job.Id}/ticket", new { });
        var ticket = (await result.Content.ReadFromJsonAsync<Ticket>())!.Token;
        client.DefaultRequestHeaders.Clear();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/backups/download/" + ticket);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4);
        using var response = await client.SendAsync(request);
        Check(response.StatusCode == HttpStatusCode.PartialContent && (await response.Content.ReadAsStringAsync()) == "PGDMP", "anonymous valid-ticket download supports bounded HTTP ranges");
        Check(response.Headers.CacheControl?.NoStore == true && response.Content.Headers.ContentDisposition?.DispositionType == "attachment", "download is a non-cacheable attachment");
        using var unknown = await client.GetAsync("api/backups/download/" + new string('0',64));
        Check(unknown.StatusCode == HttpStatusCode.NotFound, "anonymous download without a valid capability has no access");
        await app.StopAsync();
    }
    private sealed record Ticket(string Token);
    private sealed class BackupTestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Owner", out var owner)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString()), new Claim(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString())], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
    private static async Task CheckJobsAsync()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "manual-backup-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory); var original = Environment.GetEnvironmentVariable("PATH");
        try
        {
            var executable = Path.Combine(directory, "pg_dump");
            await File.WriteAllTextAsync(executable, "#!/bin/sh\nfor arg in \"$@\"; do\ncase \"$arg\" in\n--file=*) target=${arg#--file=} ;;\n--exclude*|--data-only|--table=*) exit 20 ;;\nesac\ndone\nprintf 'PGDMP-all-data-including-photos' > \"$target\"\n");
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Environment.SetEnvironmentVariable("PATH", directory + Path.PathSeparator + original);
            using var provider = new ServiceCollection().BuildServiceProvider();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["DATABASE_URL"] = "Host=localhost;Database=fake;Username=fake;Password=fake" }).Build();
            using var jobs = new ManualBackupJobs(provider.GetRequiredService<IServiceScopeFactory>(), config, NullLogger<ManualBackupJobs>.Instance);
            await jobs.StartAsync(default);
            var owner = Guid.NewGuid(); var job = jobs.Create(owner, "full")!;
            Check(jobs.Get(job.Id, Guid.NewGuid()) is null && jobs.Ticket(job.Id, Guid.NewGuid()) is null, "other users cannot view or authorize a backup download");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (jobs.Get(job.Id, owner)!.State is "queued" or "running") await Task.Delay(40, timeout.Token);
            Check(jobs.Get(job.Id, owner)!.State == "ready", "manual pg_dump includes every table and works without Telegram configuration");
            var ticket = jobs.Ticket(job.Id, owner)!;
            Check(ticket.Length == 64 && jobs.Download(new string('0',64)) is null, "download capability is unpredictable and unknown tokens are rejected");
            var download = jobs.Download(ticket)!;
            using (download.Stream) using (var reader = new StreamReader(download.Stream)) Check((await reader.ReadToEndAsync()).Contains("including-photos"), "complete archive streams to download");
            var next = jobs.Ticket(job.Id, owner)!;
            Check(jobs.Download(ticket) is null && next != ticket, "requesting another download invalidates the old ticket");
            await CheckHttpAsync(jobs);
            await jobs.StopAsync(default);
        }
        finally { Environment.SetEnvironmentVariable("PATH", original); Directory.Delete(directory, true); }
    }
}
