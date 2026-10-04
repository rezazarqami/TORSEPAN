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

foreach(var role in new[]{"Tuner","ProductionManager","Administrator"})
{
 var services=new ServiceCollection();services.AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();
 services.AddSingleton<AuthenticationStateProvider>(new FixtureAuth(role));services.AddSingleton<IJSRuntime,newJs>();services.AddScoped<TokenStorage>();
 var api=new FixtureApi();services.AddSingleton(new HttpClient(api){BaseAddress=new Uri("https://fixture.invalid/api/")});services.AddScoped<ApiClient>();services.AddScoped<DesignService>();
 await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
 async Task<string> Render(bool management=false,bool create=false)
 {
  Preview.Management=management;Preview.Create=create;
  return await renderer.Dispatcher.InvokeAsync(async()=>WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Preview>(ParameterView.Empty)).ToHtmlString()));
 }
 var ordinary=role=="Tuner";
 var html=await Render();
 Check(html.Contains("DESIGN STUDIO")&&html.Contains("design-options")&&html.Contains("register-button"),role+" renders compact registration");
 Check(html.Contains("design-tabs")==!ordinary,role+" only permitted roles see navigation");
 Check(!html.Contains("نرخ عادی")&&!html.Contains("rate-editor"),role+" registration never displays rate editor");
 html=await Render(true);
 Check(html.Contains("rate-editor")==!ordinary,role+" management state is guarded by permission");
 if(ordinary)continue;
 Check(html.Contains("حذف دیزاین")== (role=="Administrator"),role+" deletion is administrator-only");
 html=await Render(true,true);
 Check(html.Contains("ایجاد دیزاین")&&!html.Contains("design-type-picker"),role+" create and edit do not stack");
 await renderer.Dispatcher.InvokeAsync(async()=>
 {
  var instance=Preview.Instance!;Preview.Set(instance,"_newName","دیزاین تازه");Preview.Set(instance,"_newRate",120m);Preview.Set(instance,"_newExportRate",180m);
  await Preview.Call(instance,"AddTypeAsync");
  Check(api.Added&&!(bool)Preview.Get(instance,"_creating")!,role+" creation refreshes the list and returns to editor");
  var types=(List<DesignTypeDto>)Preview.Get(instance,"_types")!;
  var created=types.Single(x=>x.Name=="دیزاین تازه");
  Check((Guid)Preview.Get(instance,"_editingId")! == created.Id,role+" created design is selected");
  await Preview.Call(instance,"SaveTypeAsync",created);
  Check(api.Saved,role+" editor saves through existing authorized API");
  await Preview.Call(instance,"SearchBowlAsync");
  await Preview.Call(instance,"Toggle",FixtureApi.TypeId,true);
  await Preview.Call(instance,"AskRegister");
  await Preview.Call(instance,"ConfirmAsync");
  Check(api.Registered&&((string)Preview.Get(instance,"_message")!).Contains("با موفقیت"),role+" registration refresh preserves success feedback");
 });
}
Console.WriteLine("Design UI smoke passed.");
static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
public sealed class Preview:Designs
{
 public static bool Management,Create;public static Preview? Instance;
 protected override async Task OnInitializedAsync(){await base.OnInitializedAsync();Instance=this;Set(this,"_management",Management);Set(this,"_creating",Create);}
 public static void Set(object o,string name,object value)=>typeof(Designs).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(o,value);
 public static object? Get(object o,string name)=>typeof(Designs).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
 public static async Task Call(object o,string name,params object[] args){var result=typeof(Designs).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args);if(result is Task t)await t;}
}
sealed class FixtureAuth(string role):AuthenticationStateProvider
{
 public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()),new Claim(ClaimTypes.Role,role)],"fixture"))));
}
sealed class newJs:IJSRuntime
{
 public ValueTask<T> InvokeAsync<T>(string identifier,object?[]? args)=>ValueTask.FromResult(default(T)!);
 public ValueTask<T> InvokeAsync<T>(string identifier,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(identifier,args);
}
sealed class FixtureApi:HttpMessageHandler
{
 public static readonly Guid TypeId=Guid.Parse("11111111-1111-1111-1111-111111111111");
 readonly List<DesignTypeDto> types=[new(){Id=TypeId,Name="اسکاچی",Rate=10,ExportRate=20}];
 public bool Added,Saved,Registered;
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
 {
  var path=request.RequestUri!.AbsolutePath;object data=new{};
  if(path.EndsWith("/types")&&request.Method==HttpMethod.Post){var input=await request.Content!.ReadFromJsonAsync<DesignTypeDto>(ct);input!.Id=Guid.NewGuid();types.Add(input);Added=true;data=input;}
  else if(path.EndsWith("/types"))data=types;
  else if(path.EndsWith("/users"))data=Array.Empty<DesignUserDto>();
  else if(path.Contains("/bowls/"))data=new BowlDesignLookupDto{ProductionCode="446",BowlType="کاسه رو",MaterialName="استیل"};
  else if(request.Method==HttpMethod.Put){Saved=true;return new(HttpStatusCode.NoContent);}
  else if(path.EndsWith("/designs")&&request.Method==HttpMethod.Post){Registered=true;}
  return new(HttpStatusCode.OK){Content=JsonContent.Create(data)};
 }
}
