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
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class ProductionUiSmoke
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddAuthorizationCore(); services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, FixtureAuth>();
        services.AddSingleton<IJSRuntime, FixtureJs>();
        services.AddSingleton<NavigationManager, FixtureNavigation>();
        services.AddScoped<TokenStorage>();
        services.AddScoped(_ => new HttpClient(new FixtureApi()) { BaseAddress = new Uri("https://fixture.invalid/api/") });
        services.AddScoped<ApiClient>(); services.AddScoped<BowlService>(); services.AddScoped<HandpanService>();
        services.AddScoped<MaterialService>(); services.AddScoped<ScaleService>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, provider.GetRequiredService<ILoggerFactory>());
        async Task<string> Render(Type type) => await renderer.Dispatcher.InvokeAsync(async () =>
            WebUtility.HtmlDecode((await renderer.RenderComponentAsync(type, ParameterView.Empty)).ToHtmlString()));
        var bowls = await Render(typeof(Bowls));
        Check(bowls.Contains("PRODUCTION MANAGEMENT") && bowls.Contains("torsepan-mark-new.webp") && bowls.Contains("production-tabs"),
            "production renders the branded hero and separate navigation tabs");
        Check(bowls.Contains("BOWL-FIXTURE") && !bowls.Contains("HANDPAN-FIXTURE") && bowls.Contains("production-card-grid"),
            "default tab displays compact bowl cards and hides instrument records");
        var handpans = await Render(typeof(HandpanPreview));
        Check(handpans.Contains("HANDPAN-FIXTURE") && !handpans.Contains("BOWL-FIXTURE") && handpans.Contains("card-expand"),
            "instrument tab displays compact cards with an accessible details button");
        Check(handpans.Contains("12 notes (9+3) Full Custom Scale") && handpans.Contains("card-scale"),
            "full scale names remain available in compact cards");
        var expanded = await Render(typeof(ExpandedHandpanPreview));
        Check(!handpans.Contains("BOTTOM-LINKED") && expanded.Contains("BOTTOM-LINKED") && expanded.Contains("TOP-LINKED"), "linked codes appear when instrument details are expanded");
        Check(expanded.Contains("حذف") && expanded.Contains("مرحله قبل"),
            "expanded instrument retains administrator deletion and rollback controls");
        var empty = await Render(typeof(EmptyBowlsPreview));
        Check(empty.Contains("فیلتر کاسه‌ها") && empty.Contains("پاک کردن فیلترها") && !empty.Contains("production-card-grid"),
            "empty filtered result keeps filter and reset controls available");
        var pages = typeof(Bowls).GetMethod("VisiblePages", BindingFlags.Static | BindingFlags.NonPublic)!;
        var middle = ((IEnumerable<int>)pages.Invoke(null, [50, 1000])!).ToArray();
        var last = ((IEnumerable<int>)pages.Invoke(null, [1000, 1000])!).ToArray();
        Check(middle.SequenceEqual([48, 49, 50, 51, 52]) && last.SequenceEqual([996, 997, 998, 999, 1000]),
            "large lists keep pagination compact and include current/last pages");
    }
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public sealed class HandpanPreview : Bowls
    {
        protected override async Task OnInitializedAsync()
        { await base.OnInitializedAsync(); typeof(Bowls).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, "handpans"); }
    }
    public sealed class ExpandedHandpanPreview : Bowls
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            typeof(Bowls).GetField("_tab", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, "handpans");
            var handpans = (List<HandpanDto>)typeof(Bowls).GetField("_handpans", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
            ((HashSet<Guid>)typeof(Bowls).GetField("_expandedHandpans", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!).Add(handpans[0].Id);
        }
    }
    public sealed class EmptyBowlsPreview : Bowls
    {
        protected override async Task OnInitializedAsync()
        { await base.OnInitializedAsync(); typeof(Bowls).GetField("_bowls", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(this, new List<BowlDto>()); }
    }
    private sealed class FixtureAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Administrator")], "test"))));
    }
    private sealed class FixtureNavigation : NavigationManager
    {
        public FixtureNavigation() => Initialize("https://fixture.invalid/", "https://fixture.invalid/bowls");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
    private sealed class FixtureJs : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => ValueTask.FromResult(default(T)!);
        public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(T)!);
    }
    private sealed class FixtureApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            object data = request.RequestUri!.AbsolutePath switch
            {
                "/api/bowls" => new PagedResult<BowlDto> { Page = 1, PageSize = 20, TotalItems = 1000,
                    Items = [new BowlDto { Id = Guid.NewGuid(), ProductionCode = "BOWL-FIXTURE", BowlType = 1, HasNotes = true, MaterialName = "Stainless steel", ScaleName = "11 notes (9+2)", Stage = 8 }] },
                "/api/handpans" => new[] { new HandpanDto { Id = Guid.NewGuid(), SerialNumber = "HANDPAN-FIXTURE", TopBowlCode = "TOP-LINKED", BottomBowlCode = "BOTTOM-LINKED", MaterialName = "Stainless steel", ScaleName = "12 notes (9+3) Full Custom Scale", Stage = 12 } },
                "/api/materials" => Array.Empty<MaterialDto>(),
                "/api/scales" => Array.Empty<ScaleDto>(),
                _ => throw new Exception("Unexpected fixture request " + request.RequestUri)
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(data) });
        }
    }
}
