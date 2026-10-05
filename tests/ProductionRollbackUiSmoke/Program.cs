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

var api = new FixtureApi();
var services = new ServiceCollection();
services.AddLogging(); services.AddAuthorizationCore(); services.AddCascadingAuthenticationState();
services.AddSingleton<AuthenticationStateProvider, FixtureAuth>();
services.AddSingleton<IJSRuntime, FixtureJs>(); services.AddSingleton<NavigationManager, FixtureNavigation>();
services.AddScoped<TokenStorage>(); services.AddSingleton(new HttpClient(api) { BaseAddress = new Uri("https://fixture.invalid/api/") });
services.AddScoped<ApiClient>(); services.AddScoped<BowlService>(); services.AddScoped<HandpanService>();
services.AddScoped<MaterialService>(); services.AddScoped<ScaleService>();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
await renderer.Dispatcher.InvokeAsync(async () =>
{
    var rendered = await renderer.RenderComponentAsync<Preview>(ParameterView.Empty);
    var instance = Preview.Instance!;
    string Html() => WebUtility.HtmlDecode(rendered.ToHtmlString());
    var bowl = api.Bowls[0];
    await instance.Call("RequestRollback", bowl);
    instance.Refresh();
    Check(Html().Contains("role=\"alertdialog\"") && Html().Contains("BOWL-446") && Html().Contains("در انتظار پخت"), "bowl request displays the actual previous stage in an in-app dialog");
    Check(api.Posts.Count == 0, "opening confirmation makes no mutation request");
    await instance.Call("CancelRollback"); instance.Refresh();
    Check(!Html().Contains("role=\"alertdialog\"") && api.Posts.Count == 0, "cancel closes the dialog without rolling back");
    await instance.Call("RequestRollback", bowl);
    await instance.Call("ConfirmRollbackAsync"); instance.Refresh();
    Check(api.Posts.Single() == $"/api/bowls/{bowl.Id}/rollback" && Html().Contains("به «در انتظار پخت» برگشت"), "confirmed bowl rollback uses the bowl API and displays success");
    await instance.Call("ConfirmRollbackAsync");
    Check(api.Posts.Count == 1, "a completed confirmation cannot submit again");

    var linked = api.Bowls[1];
    await instance.Call("RequestRollback", linked); instance.Refresh();
    Check(Html().Contains("در اتاق چسب") && Html().Contains("هر دو کاسه"), "linked bowl confirms the instrument transition and its effect on both bowls");
    api.FailPost = true;
    await instance.Call("ConfirmRollbackAsync"); instance.Refresh();
    Check(Html().Contains("role=\"alertdialog\"") && Html().Contains("بازگشت انجام نشد"), "API failures stay visible inside the confirmation dialog");
    api.FailPost = false; api.PausePost = true;
    var pending = instance.Call("ConfirmRollbackAsync");
    await api.Started.Task;
    var before = api.Posts.Count;
    await instance.Call("ConfirmRollbackAsync"); await instance.Call("CancelRollback");
    Check(api.Posts.Count == before && instance.Get("_rollbackHandpan") is not null, "busy confirmation rejects duplicate submits and cancellation");
    api.Release.TrySetResult(); await pending; instance.Refresh();
    Check(api.Posts.Last() == $"/api/production/{api.Handpan.Id}/rollback" && !Html().Contains("role=\"alertdialog\""), "linked bowl rolls back through the production API after confirmation");

    await instance.Call("RequestRollback", api.Handpan); instance.Refresh();
    Check(Html().Contains("در اتاق چسب"), "direct instrument rollback also previews its target stage");
    await instance.Call("CancelRollback");
    await instance.Call("RequestRollback", new BowlDto { ProductionCode = "NEW", Stage = 2 }); instance.Refresh();
    Check(Html().Contains("بازگشت به مرحله قبل امکان‌پذیر نیست") && !Html().Contains("role=\"alertdialog\""), "unsupported stages give visible feedback without offering an invalid action");
    api.PausePost = false; api.FailRefresh = true;
    await instance.Call("RequestRollback", bowl); await instance.Call("ConfirmRollbackAsync"); instance.Refresh();
    Check(Html().Contains("برگشت") && Html().Contains("صفحه را تازه کنید") && !Html().Contains("role=\"alertdialog\""), "refresh failure after a successful rollback does not offer a second rollback");
});
Console.WriteLine("Production rollback UI smoke passed.");
static void Check(bool result, string message) { if (!result) throw new Exception(message); Console.WriteLine("PASS " + message); }

public sealed class Preview : Bowls
{
    public static Preview? Instance;
    protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Instance = this; }
    public void Refresh() => StateHasChanged();
    public object? Get(string name) => typeof(Bowls).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this);
    public async Task Call(string name, params object[] args)
    {
        var method = typeof(Bowls).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => x.Name == name && x.GetParameters().Length == args.Length && x.GetParameters().Select((p, i) => p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
        if (method.Invoke(this, args) is Task task) await task;
    }
}
sealed class FixtureAuth : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Administrator")], "fixture"))));
}
sealed class FixtureNavigation : NavigationManager
{
    public FixtureNavigation() => Initialize("https://fixture.invalid/", "https://fixture.invalid/bowls");
    protected override void NavigateToCore(string uri, bool forceLoad) { }
}
sealed class FixtureJs : IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string id, object?[]? args)
    {
        if (id == "confirm") throw new Exception("Native browser confirmation must not be used");
        return ValueTask.FromResult(default(T)!);
    }
    public ValueTask<T> InvokeAsync<T>(string id, CancellationToken ct, object?[]? args) => InvokeAsync<T>(id, args);
}
sealed class FixtureApi : HttpMessageHandler
{
    public List<string> Posts = [];
    public bool FailPost, PausePost, FailRefresh;
    public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public List<BowlDto> Bowls = [new() { Id = Guid.NewGuid(), ProductionCode = "BOWL-446", Stage = 8 }, new() { Id = Guid.NewGuid(), ProductionCode = "LINKED-447", Stage = 12 }];
    public HandpanDto Handpan = new() { Id = Guid.NewGuid(), SerialNumber = "HANDPAN-447", TopBowlCode = "LINKED-447", BottomBowlCode = "LINKED-448", Stage = 12 };
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Post)
        {
            Posts.Add(path);
            if (PausePost) { Started.TrySetResult(); await Release.Task; }
            return new(FailPost ? HttpStatusCode.BadRequest : HttpStatusCode.NoContent);
        }
        if (FailRefresh) return new(HttpStatusCode.ServiceUnavailable);
        object data = path switch
        {
            "/api/bowls" => new PagedResult<BowlDto> { Page = 1, PageSize = 20, TotalItems = 2, Items = Bowls },
            "/api/handpans" => new[] { Handpan },
            "/api/materials" => Array.Empty<MaterialDto>(),
            "/api/scales" => Array.Empty<ScaleDto>(),
            _ => throw new Exception("Unexpected request " + path)
        };
        return new(HttpStatusCode.OK) { Content = JsonContent.Create(data) };
    }
}
