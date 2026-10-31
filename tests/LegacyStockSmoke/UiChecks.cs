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

static class UiChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
    public static async Task Run()
    {
        var api=new FixtureApi();var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();
        services.AddSingleton<AuthenticationStateProvider>(new FixtureAuth());services.AddSingleton<IJSRuntime,FixtureJs>();services.AddSingleton<NavigationManager,FixtureNavigation>();
        services.AddSingleton(new HttpClient(api){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<UserApiClient>();services.AddScoped<IUserService,UserService>();services.AddScoped<MaterialService>();services.AddScoped<ScaleService>();services.AddScoped<ProductionService>();services.AddScoped<LegacyStockService>();services.AddScoped<DesignService>();
        await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async()=>{
            var root=await renderer.RenderComponentAsync<LegacyPreview>(ParameterView.Empty);var view=LegacyPreview.Instance!;string Html()=>WebUtility.HtmlDecode(root.ToHtmlString());
            var dir=Environment.GetEnvironmentVariable("LEGACY_PREVIEW_DIR");if(dir is not null)Directory.CreateDirectory(dir);
            async Task Snapshot(string name){view.Refresh();if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,name+".html"),root.ToHtmlString());}
            Check(System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(Html(), "<[^>]+>", " "), @"\s+", " ").Contains("ثبت سازهای قدیمی")&&Html().Contains("legacy-code")&&!Html().Contains("id=\"legacy-bottom-code\""),"compact form asks for only the visible top code");
            Check(Html().Contains("سازنده غیرفعال (غیرفعال)"),"historical inactive staff remain selectable");
            await Snapshot("empty");var draft=(LegacyStockDraft)view.Get("_draft")!;draft.Code="OLD-0123";draft.MaterialId=api.Material;draft.ScaleId=api.Scale;draft.TopNotes=9;draft.BottomNotes=2;draft.WarehouseId=2;draft.DimpleUserId=api.Maker;
            draft.Destination="export";await view.Call("SaveAsync");view.Refresh();Check(api.Requests.Count==0&&Html().Contains("انبار صادراتی را انتخاب کنید"),"export destination is required before any write");await Snapshot("export");
            draft.ExportLocation=3;api.LoseReply=true;await view.Call("SaveAsync");view.Refresh();
            Check(api.Requests.Count==1&&Html().Contains("پاسخ ثبت دریافت نشد")&&draft.Code=="OLD-0123","lost response preserves entered data and offers safe retry");await Snapshot("retry");
            api.LoseReply=false;await view.Call("SaveAsync");view.Refresh();
            Check(api.Requests.Count==2&&api.Requests[0]==api.Requests[1]&&Html().Contains("ساز OLD-0123 ثبت شد"),"retry reuses exact immutable payload and identity then shows acknowledged success");
            Check(Html().Contains("آلمان")&&Html().Contains("/admin/production-edit?code=OLD-0123"),"success links directly to the ordinary passport and selected export stock");await Snapshot("saved");
            await view.Call("Next");view.Refresh();Check(draft.Code==""&&draft.ScaleId==api.Scale&&draft.WarehouseId==2&&Html().Contains("legacy-code"),"next stock entry clears code while retaining repeated workshop choices");
        });
    }
    sealed class FixtureApi:HttpMessageHandler
    {
        public Guid Material=Guid.NewGuid(),Scale=Guid.NewGuid(),Maker=Guid.NewGuid();public bool LoseReply;public List<LegacyStockRequest> Requests=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            object data=Array.Empty<object>();var path=request.RequestUri!.AbsolutePath;
            if(path.EndsWith("/materials"))data=new[]{new MaterialDto{Id=Material,Name="Ember Steel",Category=4}};
            else if(path.EndsWith("/scales"))data=new[]{new ScaleDto{Id=Scale,Name="D Kurd",Usage=7}};
            else if(path.EndsWith("/users"))data=new[]{new TORSEPAN.Application.Auth.Queries.GetUsers.UserDto{Id=Maker,FullName="سازنده غیرفعال",IsActive=false}};
            else if(path.EndsWith("/instrument-warehouses"))data=InstrumentWarehouseDto.Defaults();
            else if(path.EndsWith("/legacy-stock")&&request.Method==HttpMethod.Post){var payload=(await request.Content!.ReadFromJsonAsync<LegacyStockRequest>(ct))!;Requests.Add(payload);if(LoseReply)throw new HttpRequestException("fixture lost response");data=new LegacyStockReceipt(payload.RegistrationId,payload.Code,22,payload.WarehouseId,payload.ExportLocation);}
            return new(HttpStatusCode.OK){Content=JsonContent.Create(data)};
        }
    }
    sealed class FixtureAuth:AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,"Administrator")},"fixture"))));}
    sealed class FixtureJs:IJSRuntime{public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
    sealed class FixtureNavigation:NavigationManager{public FixtureNavigation(){Initialize("https://fixture.invalid/","https://fixture.invalid/admin/legacy-stock");}protected override void NavigateToCore(string uri,bool forceLoad){}}
}
sealed class LegacyPreview:LegacyStock
{
    public static LegacyPreview? Instance;protected override async Task OnInitializedAsync(){Instance=this;await base.OnInitializedAsync();}
    public object? Get(string name)=>typeof(LegacyStock).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(this);
    public async Task Call(string name){var r=typeof(LegacyStock).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,null);if(r is Task t)await t;}
    public void Refresh()=>StateHasChanged();
}
