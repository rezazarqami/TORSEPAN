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

await PackagingSmoke.RunAsync();
var output=Environment.GetEnvironmentVariable("INVENTORY_PREVIEW_DIR");if(output is not null)Directory.CreateDirectory(output);
foreach(var role in new[]{"Administrator","Tuner"})
{
 var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider>(new AuthFixture(role));services.AddSingleton<IJSRuntime,JsFixture>();services.AddSingleton<NavigationManager,NavigationFixture>();services.AddScoped<TokenStorage>();
 services.AddSingleton(new HttpClient(new ApiFixture()){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<ApiClient>();services.AddScoped<MaterialService>();services.AddScoped<BowlService>();services.AddScoped<ScaleService>();services.AddScoped<ProductionService>();
 await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
 async Task<string> Render(Type type)=>await renderer.Dispatcher.InvokeAsync(async()=>WebUtility.HtmlDecode((await renderer.RenderComponentAsync(type,ParameterView.Empty)).ToHtmlString()));
 var overview=await Render(typeof(MaterialInventoryOverview));Check(overview.Contains("MATERIAL INVENTORY")&&overview.Contains("کارتون")&&overview.Contains("مثلثی"),role+" overview includes all inventory");
 var management=await Render(typeof(Materials));Check(management.Contains("bowl-stock-inline")== (role=="Administrator"),role+" inventory controls follow existing administrator permissions");
 if(role=="Administrator")
 {
  var supplies=await Render(typeof(SupplyPreview));var create=await Render(typeof(CreatePreview));
  Check(supplies.Contains("other-stock-inline")&&supplies.Contains("حد هشدار"),"other items render compact stock and threshold controls");
  Check(create.Contains("material-create embedded")&&!create.Contains("bowl-stock-inline"),"create form is embedded in management rather than stacked below editors");
  if(output is not null)foreach(var (name,html) in new[]{("overview",overview),("management",management),("supplies",supplies),("create",create)})await File.WriteAllTextAsync(Path.Combine(output,name+".html"),html);
 }
 var packaging=await Render(typeof(PackagingPreview));
 Check(packaging.Contains("کارتون")&&packaging.Contains("مثلثی")&&packaging.Contains("به‌روزرسانی اقلام"),"new other-category items are included in packaging checkboxes");
 await renderer.Dispatcher.InvokeAsync(async()=>
 {
  var instance=PackagingPreview.Instance!;var materials=(List<MaterialDto>)Fields.Get(instance,"_packagingMaterials",typeof(Dimpling))!;materials.Clear();
  await Fields.Call(instance,"LoadPackagingMaterialsAsync");
  Check(materials.Any(x=>x.Name=="کارتون")&&materials.Any(x=>x.Name=="مثلثی"),"refresh restores newly added packaging materials");
 });
}
Console.WriteLine("Inventory and packaging smoke passed.");
static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
public sealed class SupplyPreview:Materials {protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Fields.Set(this,"_category",3,typeof(Materials));Fields.Set(this,"_selectedId",ApiFixture.CartonId,typeof(Materials));}}
public sealed class CreatePreview:Materials {protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Fields.Set(this,"_adding",true,typeof(Materials));}}
public sealed class PackagingPreview:Dimpling
{
 public static PackagingPreview? Instance;
 protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;Fields.Set(this,"_bowl",new DimpleBowlDto{ProductionCode="446",Stage=16,BowlType=1,HandpanId=Guid.NewGuid()},typeof(Dimpling));}
}
static class Fields
{
 public static object? Get(object o,string name,Type t)=>t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
 public static void Set(object o,string name,object value,Type t)=>t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(o,value);
 public static async Task Call(object o,string name){var result=typeof(Dimpling).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,null);if(result is Task t)await t;}
}
sealed class AuthFixture(string role):AuthenticationStateProvider {public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,role)],"fixture"))));}
sealed class JsFixture:IJSRuntime {public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
sealed class NavigationFixture:NavigationManager {public NavigationFixture(){Initialize("https://fixture.invalid/","https://fixture.invalid/warehouse/materials");}protected override void NavigateToCore(string uri,bool forceLoad){}}
sealed class ApiFixture:HttpMessageHandler
{
 public static readonly Guid CartonId=Guid.Parse("22222222-2222-2222-2222-222222222222");
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
 {
  object data=Array.Empty<object>();if(request.RequestUri!.AbsolutePath.EndsWith("/materials"))
  {
   var items=new List<MaterialDto>();foreach(var (name,i) in new[]{"Ember Steel A","Ember Steel B","Mini Pan","NITRIDE","Stainless Steel","تیتانیوم"}.Select((x,i)=>(x,i)))items.Add(new(){Id=Guid.NewGuid(),Name=name,Category=4,TopBowlQuantity=20+i,BottomBowlQuantity=40+i,TopBowlCodeTemplate="ST-00000",BottomBowlCodeTemplate="SB-00000"});
   foreach(var name in new[]{"کارتون","مثلثی","روغن","دستمال","پایه","پن گارد","سافت کیس","هارد کیس"})items.Add(new(){Id=name=="کارتون"?CartonId:Guid.NewGuid(),Name=name,Category=3,Quantity=10,LowStockThreshold=3});data=items;
  }
  return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(data)});
 }
}
