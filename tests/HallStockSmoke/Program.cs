using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TORSEPAN.Panel.Components.Layout;
using TORSEPAN.Panel.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
var material = Guid.NewGuid();
var bowls = new List<Bowl>(); var handpans = new List<Handpan>();
foreach (var stage in Enum.GetValues<ProductionStage>())
{
    foreach (var type in new[] { BowlType.Top, BowlType.Bottom })
    {
        var bowl = new Bowl($"{type}-{stage}", type, true, InstrumentType.Standard, material);
        bowl.ChangeStage(stage); bowls.Add(bowl);
    }
    var handpan = new Handpan(Guid.NewGuid(), "HP-" + stage);
    handpan.ChangeStage(stage); handpans.Add(handpan);
}
var assembledTop = new Bowl("ASSEMBLED-TOP", BowlType.Top, true, InstrumentType.Standard, material);
var assembledBottom = new Bowl("ASSEMBLED-BOTTOM", BowlType.Bottom, false, InstrumentType.Standard, material);
// Even historical stale pre-glue stages must not double-count an assembled pair.
assembledTop.ChangeStage(ProductionStage.WaitingForGlue); assembledBottom.ChangeStage(ProductionStage.WaitingForGlue);
bowls.AddRange([assembledTop, assembledBottom]);
var assembly = new HandpanAssembly(assembledTop.Id, assembledBottom.Id);
var stock = ProductionHallStockResponse.Calculate(bowls, handpans, [assembly]);
Check(stock.TopBowls == 6 && stock.BottomBowls == 6 && stock.Handpans == 4,
    "count six loose bowl queues by type and exactly the four instrument queues; exclude warehouses and assembled bowls");
var ready = handpans.Single(x => x.Stage == ProductionStage.WaitingForPackaging);
ready.ChangeStage(ProductionStage.FinishedWarehouse);
stock = ProductionHallStockResponse.Calculate(bowls, handpans, [assembly]);
Check(stock.Handpans == 3, "packaging completion removes the instrument from the hall immediately");
var export = bowls.Single(x => x.Stage == ProductionStage.WaitingForExportPackaging && x.BowlType == BowlType.Bottom);
export.ChangeStage(ProductionStage.ExportWarehouse);
stock = ProductionHallStockResponse.Calculate(bowls, handpans, [assembly]);
Check(stock.BottomBowls == 5, "export bowls count before packaging and disappear on entering export warehouse");
var empty = ProductionHallStockResponse.Calculate([], [], []);
Check(empty.TopBowls == 0 && empty.BottomBowls == 0 && empty.Handpans == 0, "empty production shows zeros");

var services = new ServiceCollection(); services.AddLogging();
services.AddScoped<AuthenticationStateProvider, FixtureAuth>();
services.AddScoped<PersonalWorkspaceService>();
services.AddScoped<IAuthService, FixtureAuthService>();
services.AddSingleton<IJSRuntime, FixtureJs>(); services.AddSingleton<NavigationManager, FixtureNavigation>();
services.AddScoped<TokenStorage>();
services.AddScoped(_ => new HttpClient(new FixtureApi()) { BaseAddress = new Uri("https://fixture.invalid/api/") });
services.AddScoped<ApiClient>(); services.AddScoped<ProductionService>();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Dashboard>()).ToHtmlString());
var text = WebUtility.HtmlDecode(html);
Check(text.Contains("کاسه رو") && text.Contains("کاسه زیر") && text.Contains("hall-stock-handpans") && text.Contains(">123</dd>") && text.Contains(">45</dd>") && text.Contains(">67</dd>"), "dashboard renders all three counts from API");
Check(text.IndexOf("ورودی انبار این ماه") < text.IndexOf("hall-stock-heading"), "hall totals appear below warehouse queue");
Check(!text.Contains("dashboard-hero-logo"), "dashboard has no duplicate hero mark");
var header = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Topbar>()).ToHtmlString());
Check(header.Contains("topbar-workshop-mark") && !header.Contains("topbar-brandmark"), "header uses the silver workshop mark without the old cream tile");
var directory = Environment.GetEnvironmentVariable("TORSEPAN_HALL_PREVIEW_DIR");
if (directory is not null) { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory, "dashboard.html"), html); await File.WriteAllTextAsync(Path.Combine(directory, "header.html"), header); }

FixtureApi.MissingHallStock = true;
var unavailable = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Dashboard>()).ToHtmlString());
Check(WebUtility.HtmlDecode(unavailable).Contains("آمار موجودی سالن هنوز دریافت نشده است") && !unavailable.Contains("hall-stock-grid"), "older API responses show unavailable stock instead of misleading zero counts");

sealed class FixtureApi : HttpMessageHandler
{
    public static bool MissingHallStock { get; set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri!.AbsolutePath != "/api/production/dashboard") throw new Exception("Unexpected " + request.RequestUri);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ProductionDashboardDto
        {
            HallStock = MissingHallStock ? null : new() { TopBowls = 123, BottomBowls = 45, Handpans = 67 },
            Queues = [new() { Stage = "ورودی انبار این ماه", Codes = ["WAREHOUSE-1"], Items = [new() { Code = "WAREHOUSE-1" }] }],
            CurrentPersianMonthTitle = "مهر 1405", DaysInMonth = 30, SelectedPersianDay = 1
        }) });
    }
}
sealed class FixtureNavigation : NavigationManager
{
    public FixtureNavigation() => Initialize("https://fixture.invalid/", "https://fixture.invalid/");
    protected override void NavigateToCore(string uri, bool forceLoad) { }
}
sealed class FixtureJs : IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => ValueTask.FromResult(default(T)!);
    public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(T)!);
}

sealed class FixtureAuth : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "رضا ضرغامی")], "fixture"))));
}

sealed class FixtureAuthService : IAuthService
{
    public bool IsAuthenticated => true;
    public string? Token => null;
    public string? UserName => "fixture";
    public string? FullName => "رضا ضرغامی";
    public IReadOnlyList<string> Roles => [];
    public Task LogoutAsync() => Task.CompletedTask;
    public Task<TORSEPAN.Application.Auth.Commands.Login.LoginResult> LoginAsync(TORSEPAN.Application.Auth.Commands.Login.LoginCommand command) => throw new NotSupportedException();
}
