using System.Net;
using System.Net.Http.Json;
using System.Reflection;
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

static class ProcessFlowChecks
{
 public static async Task Run()
 {
  var api=new FlowApi();var services=new ServiceCollection().AddLogging();services.AddAuthorizationCore();services.AddCascadingAuthenticationState();services.AddSingleton<AuthenticationStateProvider,FixtureAuth>();services.AddSingleton<IJSRuntime,FixtureJs>();services.AddSingleton<NavigationManager,FixtureNavigation>();
  services.AddSingleton(new HttpClient(api){BaseAddress=new("https://fixture.invalid/api/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<BowlService>();services.AddScoped<ScaleService>();services.AddScoped<MaterialService>();services.AddScoped<ProductionService>();services.AddScoped<HandpanPhotoService>();
  await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
  await renderer.Dispatcher.InvokeAsync(async()=>{
   var root=await renderer.RenderComponentAsync<FlowPreview>();var view=FlowPreview.Instance!;string Html()=>WebUtility.HtmlDecode(root.ToHtmlString());
   var dir=Environment.GetEnvironmentVariable("PROCESS_PREVIEW_DIR");if(dir is not null)Directory.CreateDirectory(dir);
   async Task Snapshot(string name){view.Refresh();if(dir is not null)await File.WriteAllTextAsync(Path.Combine(dir,name+".html"),root.ToHtmlString());}
   await view.Call("CompleteFinalTuneAsync");await Snapshot("confirmation");Check(Html().Contains("operation-confirm-dialog")&&api.Writes==0,"confirmation is shown before any write");
   api.Fail=true;await view.Call("ConfirmOperationAsync");view.Refresh();Check(view.Get("_successMessage") is null&&Html().Contains("SS-00090")&&api.Writes==1,"failed operation retains code and shows no success");
   api.Fail=false;api.Gate=new(TaskCreationOptions.RunContinuationsAsynchronously);await view.Call("CompleteFinalTuneAsync");var save=view.Call("ConfirmOperationAsync");
   await view.Call("ConfirmOperationAsync");await view.Call("HandleKeyDown",new KeyboardEventArgs{Key="Enter"});Check(api.Writes==2&&api.Lookups==0,"duplicate confirmation and Enter cannot race an in-flight write");api.Gate.SetResult();await save;
   await Snapshot("success");Check(Html().Contains("process-operation-success")&&!Html().Contains("aria-modal=\"true\"")&&Html().Contains("id=\"passport-code\"")&&Html().Contains("value=\"SS-00090\""),"inline success and same populated code input coexist without a blocking modal");
   Check((bool)view.Get("_focusCodeAfterOperation")!,"successful operation requests focus back to the existing code input");
   await view.Call("HandleKeyDown",new KeyboardEventArgs{Key="Enter"});await Snapshot("next-stage");Check(api.Lookups==1&&api.Writes==2&&((DimpleBowlDto)view.Get("_bowl")!).Stage==14&&view.Get("_successMessage") is null,"Enter reloads the same code into QC without repeating the completed operation");
  });
 }
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 internal static DimpleBowlDto Bowl(int stage)=>new(){ProductionCode="SS-00090",HandpanCode="SS-00090",HandpanId=Guid.Parse("00000000-0000-0000-0000-000000000090"),ScaleName="D Kurd 12",Stage=stage};
 sealed class FlowApi:HttpMessageHandler{
  public int Writes,Lookups;public bool Fail;public TaskCompletionSource? Gate;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){
   object data=Array.Empty<object>();var path=request.RequestUri!.AbsolutePath;
   if(request.Method==HttpMethod.Post){if(!path.EndsWith("/final-tune/complete"))throw new Exception(path);Writes++;if(Fail)return new(HttpStatusCode.InternalServerError);if(Gate is not null)await Gate.Task;data=Bowl(14);}
   else if(path.Contains("/bowls/dimpling/")){Lookups++;data=Bowl(14);}
   return new(HttpStatusCode.OK){Content=JsonContent.Create(data)};
  }
 }
}
sealed class FlowPreview:Dimpling{
 public static FlowPreview? Instance;
 protected override Task OnInitializedAsync(){Instance=this;Set("_productionCode","SS-00090");Set("_bowl",ProcessFlowChecks.Bowl(12));return Task.CompletedTask;}
 public object? Get(string n)=>typeof(Dimpling).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(this);
 public void Set(string n,object? v)=>typeof(Dimpling).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(this,v);
 public async Task Call(string n,params object?[] args){if(typeof(Dimpling).GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(this,args) is Task t)await t;}
 public void Refresh()=>StateHasChanged();
}
