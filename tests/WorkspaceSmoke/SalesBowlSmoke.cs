using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Sales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;

internal static class SalesBowlSmoke
{
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }

    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var user = new User("bowl-maker", "سازنده آزمایشی");
        var material = new Material("Test steel");
        var bowls = new[] {
            new Bowl("SOLD-TOP", BowlType.Top, true, InstrumentType.Standard, material.Id),
            new Bowl("SOLD-PLAIN", BowlType.Bottom, false, InstrumentType.Standard, material.Id),
            new Bowl("SOLD-NOTES", BowlType.Bottom, true, InstrumentType.Standard, material.Id)
        };
        foreach (var bowl in bowls) bowl.ChangeStage(ProductionStage.Sold);
        var unsold = new Bowl("UNSOLD", BowlType.Bottom, false, InstrumentType.Standard, material.Id);
        db.AddRange(user, material, unsold);
        db.AddRange(bowls);
        foreach (var bowl in bowls)
            db.Add(new ProductionEvent(null, null, bowl.Id, user.Id, ProductionAction.Sale, EventResult.Completed, null, "Export sale"));
        foreach (var action in new[] { ProductionAction.Shape, ProductionAction.Tune, ProductionAction.FineTune })
            db.Add(new ProductionEvent(null, null, bowls[0].Id, user.Id, action, EventResult.Completed, null, "Completed"));
        db.Add(new ProductionEvent(null, null, bowls[1].Id, user.Id, ProductionAction.Shape, EventResult.Completed, null, "Other bowl"));
        await db.SaveChangesAsync();
        var unit = new UnitOfWork(db, new UserRepository(db), null!, null!, null!,
            new HandpanRepository(db), new HandpanAssemblyRepository(db), new BowlRepository(db), new MaterialRepository(db),
            new ScaleRepository(db), new ProductionEventRepository(db));
        var sales = await new GetSalesQueryHandler(unit).Handle(new(), default);
        Check(sales.Count == 3 && sales.All(x => x.IsBowl), "sales list excludes unsold bowls");
        var rows = JsonSerializer.Deserialize<List<SaleItemDto>>(JsonSerializer.Serialize(sales))!;
        Check(rows.Single(x => x.SerialNumber == "SOLD-PLAIN") is { BowlType: 2, HasNotes: false } &&
              rows.Single(x => x.SerialNumber == "SOLD-NOTES") is { BowlType: 2, HasNotes: true } &&
              rows.Single(x => x.SerialNumber == "SOLD-TOP").BowlType == 1, "bowl classification survives API-to-panel serialization");
        var detail = await new GetBowlForDimpleQueryHandler(unit).Handle(new("SOLD-TOP"), default);
        Check(detail.IsSuccess && detail.Value!.History.Count == 4 &&
              detail.Value.History.All(x => x.PerformedBy == user.FullName) &&
              detail.Value.History.Any(x => x.ActionTitle == "فاین تیون"), "sold bowl retains shape, tune and fine tune with the maker's name");
        Check(typeof(BowlsController).GetMethod("GetForDimpling")!.GetCustomAttribute<AuthorizeAttribute>()!
            .Roles!.Split(',').Contains("SalesAdmin"), "sales administrators can read bowl history");
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var page = new Sales();
        typeof(Sales).GetField("_items", flags)!.SetValue(page, rows);
        typeof(Sales).GetField("_saleSegment", flags)!.SetValue(page, 2);
        int Count(int type, bool? notes = null) => (int)typeof(Sales).GetMethod("ExportBowlCount", flags)!.Invoke(page, [type, notes])!;
        Check(Count(2, false) == 1 && Count(2, true) == 1 && Count(1) == 1, "three export bowl counters separate plain bottoms, noted bottoms and tops");
        typeof(Sales).GetField("_search", flags)!.SetValue(page, "SOLD-NOTES");
        Check(Count(2, false) == 0 && Count(2, true) == 1 && Count(1) == 0, "counters track the filtered sales list");
        var fixture = new HistoryApi(JsonSerializer.Deserialize<DimpleBowlDto>(JsonSerializer.Serialize(detail.Value))!);
        var services = new ServiceCollection();
        services.AddLogging(); services.AddAuthorizationCore(); services.AddCascadingAuthenticationState();
        services.AddSingleton<AuthenticationStateProvider, FixtureAuth>();
        services.AddSingleton<IJSRuntime, FixtureJs>();
        services.AddScoped<TokenStorage>();
        services.AddScoped(_ => new HttpClient(fixture) { BaseAddress = new Uri("https://fixture.invalid/api/") });
        services.AddScoped<ApiClient>(); services.AddScoped<BowlService>(); services.AddScoped<ProductionService>();
        services.AddScoped<AccountingService>(); services.AddScoped<SalesAttributionService>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        SalesPreview.Rows = rows;
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            WebUtility.HtmlDecode((await renderer.RenderComponentAsync<SalesPreview>(ParameterView.Empty)).ToHtmlString()));
        Check(fixture.Requests == 1, "opening a sold bowl fetches its history once and reopening uses the cache");
        Check(html.Contains("سابقه ساخت کاسه SOLD-TOP") && html.Contains("فاین تیون") && html.Contains(user.FullName) &&
              html.Contains("کاسه زیر ساده") && html.Contains("کاسه زیر نوت‌دار") && html.Contains("کاسه رو"),
            "expanded sales UI renders production history and all three category counters");
    }

    public sealed class SalesPreview : Sales
    {
        public static List<SaleItemDto> Rows = [];
        protected override async Task OnInitializedAsync()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(Sales).GetField("_items", flags)!.SetValue(this, Rows);
            typeof(Sales).GetField("_saleSegment", flags)!.SetValue(this, 2);
            typeof(Sales).GetField("_loading", flags)!.SetValue(this, false);
            var item = Rows.Single(x => x.SerialNumber == "SOLD-TOP");
            var toggle = typeof(Sales).GetMethod("ToggleDetailsAsync", flags)!;
            for (var i = 0; i < 3; i++) await (Task)toggle.Invoke(this, [item])!;
        }
    }
    private sealed class HistoryApi(DimpleBowlDto detail) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Check(request.RequestUri!.AbsolutePath == "/api/bowls/dimpling/SOLD-TOP", "bowl expansion uses the production history endpoint");
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(detail) });
        }
    }
    private sealed class FixtureAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "SalesAdmin")], "test"))));
    }
    private sealed class FixtureJs : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => ValueTask.FromResult(default(T)!);
        public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(T)!);
    }
}
