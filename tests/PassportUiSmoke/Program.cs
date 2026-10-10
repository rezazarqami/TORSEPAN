using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Components.Shared;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Auth;

var services = new ServiceCollection();
services.AddLogging(); services.AddAuthorizationCore(); services.AddCascadingAuthenticationState();
services.AddSingleton<AuthenticationStateProvider, FixtureAuth>();
services.AddSingleton<IJSRuntime, FixtureJs>(); services.AddSingleton<NavigationManager, FixtureNavigation>();
services.AddScoped<TokenStorage>(); services.AddSingleton(new HttpClient(new FixtureApi()) { BaseAddress = new Uri("https://fixture.invalid/api/") });
services.AddScoped<ApiClient>(); services.AddScoped<BowlService>(); services.AddScoped<ProductionService>();
services.AddScoped<MaterialService>(); services.AddScoped<ScaleService>(); services.AddScoped<HandpanPhotoService>();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
await renderer.Dispatcher.InvokeAsync(async () =>
{
    var view = await renderer.RenderComponentAsync<PassportPreview>();
    var html = WebUtility.HtmlDecode(view.ToHtmlString());
    Check(!System.Text.RegularExpressions.Regex.IsMatch(html, "<h3[^>]*>شناسنامه ساز</h3>") && !html.Contains("برگرفته از") && !html.Contains("دیمپل ←"), "redundant headings and stage strip removed");
    Check(html.Contains("passport-codes") && html.Contains("196") && html.Contains("(اسیدی)") && html.Contains("D Kurd 14") && html.Contains("Design") && html.Contains("Status"), "passport has both codes, bottom design, scale, design and status");
    Check(html.Contains("شیپ کاسه رو: شایان نجفی") && html.Contains("شیپ کاسه زیر: محمدرضا رنجبر"), "separate bowl attribution is preserved");
    Check(html.Contains("passport-event-date") && html.Contains("تاریخ</th>"), "history retains dates and times in its dedicated column");
    Check(html.Contains("تصاویر ساز") && !html.Contains("عکس‌های شناسنامه") && !html.Contains("اختیاری"), "photo panel has one heading and still renders camera and gallery");
    Check(html.Contains("دوربین") && html.Contains("گالری"), "optional photo upload remains available");
    var navigation = (FixtureNavigation)provider.GetRequiredService<NavigationManager>();
    foreach (var route in new[] { "/dimpling", "/production/process", "/production/process/?code=855" })
    {
        navigation.Set(route);
        var search = await renderer.RenderComponentAsync<GlobalProductionSearch>();
        Check(!search.ToHtmlString().Contains("بررسی مرحله"), "duplicate search hidden at " + route);
    }
    navigation.Set("/sales");
    Check((await renderer.RenderComponentAsync<GlobalProductionSearch>()).ToHtmlString().Contains("بررسی مرحله"), "global search retained on other pages");
    if (Environment.GetEnvironmentVariable("PASSPORT_PREVIEW_PATH") is { } path) await File.WriteAllTextAsync(path, view.ToHtmlString());
    foreach (var type in new[] { 1, 2 })
    {
        var bowlView = await renderer.RenderComponentAsync<PassportPreview>(ParameterView.FromDictionary(new Dictionary<string,object?> { [nameof(PassportPreview.BowlType)] = type }));
        var bowlHtml = WebUtility.HtmlDecode(bowlView.ToHtmlString());
        Check(bowlHtml.Contains("BOWL ID") && bowlHtml.Contains("passport-primary-info") && bowlHtml.Contains("کد کاسه") && bowlHtml.Contains("SS-00090"), "loose bowl uses the same branded blue passport card");
        Check(bowlHtml.Contains(type == 1 ? "کاسه رو" : "کاسه زیر") && !bowlHtml.Contains("کاسه بالا") && !bowlHtml.Contains("production-highlights"), "bowl type labels are explicit and legacy highlights removed");
        Check(bowlHtml.Contains("Scale:") && bowlHtml.Contains("Design:") && bowlHtml.Contains("Status:"), "shared information rows include label separators");
        if (Environment.GetEnvironmentVariable("PASSPORT_PREVIEW_PATH") is { } bowlPath) await File.WriteAllTextAsync(bowlPath + ".bowl-" + type + ".html", bowlView.ToHtmlString());
    }

});
static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
public sealed class PassportPreview : Dimpling
{
    [Parameter] public int BowlType { get; set; }
    protected override Task OnInitializedAsync()
    {
        var record = new DimpleBowlDto { HandpanId = Guid.NewGuid(), HandpanCode = "855", BottomBowlCode = "196", BottomDesignName = "اسیدی", DesignName = "ساده", ScaleName = "D Kurd 14", Stage = 18 };
        if (BowlType != 0)
        {
            record.HandpanId = null; record.HandpanCode = ""; record.ProductionCode = "SS-00090";
            record.BowlType = BowlType; record.Stage = 8; record.ScaleName = "D Kurd 9";
        }
        record.History.Add(new() { ActionTitle = "شیپ", PerformedAt = new DateTime(2026,10,4,9,30,0,DateTimeKind.Utc), BowlPerformers = [new() { Label = "شیپ کاسه رو", PerformedBy = "شایان نجفی" }, new() { Label = "شیپ کاسه زیر", PerformedBy = "محمدرضا رنجبر", Details = "کشش توسط شاهین" }] });
        record.History.Add(new() { ActionTitle = "فاین تیون", PerformedBy = "رضا ضرغامی", PerformedAt = DateTime.UtcNow });
        typeof(Dimpling).GetField("_bowl",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this, record);
        return Task.CompletedTask;
    }
}
sealed class FixtureAuth : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,"Administrator")],"fixture"))));
}
sealed class FixtureNavigation : NavigationManager
{
    public FixtureNavigation() => Initialize("https://fixture.invalid/", "https://fixture.invalid/production/process");
    public void Set(string route) => Uri = "https://fixture.invalid" + route;
    protected override void NavigateToCore(string uri, bool forceLoad) { }
}
sealed class FixtureJs : IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => ValueTask.FromResult(default(T)!);
    public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args) => InvokeAsync<T>(id,args);
}
sealed class FixtureApi : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<HandpanPhotoDto>()) });
}
