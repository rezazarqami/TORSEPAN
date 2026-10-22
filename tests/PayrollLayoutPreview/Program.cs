using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

var output=Environment.GetEnvironmentVariable("PAYROLL_PREVIEW_DIR") ?? throw new Exception("Set PAYROLL_PREVIEW_DIR");Directory.CreateDirectory(output);
var services=new ServiceCollection().AddLogging();services.AddSingleton<IJSRuntime,FixtureJs>();services.AddSingleton<NavigationManager,FixtureNavigation>();
services.AddSingleton(new HttpClient(new FixtureApi()){BaseAddress=new("https://fixture.invalid/api/")});services.AddScoped<TokenStorage>();services.AddScoped<ApiClient>();services.AddScoped<PayrollService>();services.AddScoped<MaterialService>();services.AddScoped<ScaleService>();
await using var provider=services.BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
await renderer.Dispatcher.InvokeAsync(async()=>{
 var page=await renderer.RenderComponentAsync<Payroll>(ParameterView.Empty);
 await File.WriteAllTextAsync(Path.Combine(output,"payroll.html"),page.ToHtmlString());
});
Console.WriteLine("Rendered production payroll page with normal, Custom, export, long scale names and large amounts.");
sealed class FixtureApi:HttpMessageHandler{
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){
  if(request.Method!=HttpMethod.Get)throw new Exception("Preview must never write.");
  object data=Array.Empty<object>();
  if(request.RequestUri!.AbsolutePath.EndsWith("/payroll")){
   var id=Guid.NewGuid();
   data=new PayrollDto{From=new(2026,10,1),To=new(2026,10,22),Lines=[
    new(){UserId=id,UserName="آبتین ترسه",Action=6,ActionTitle="تیون",MaterialName="Stainless Steel",BowlType=1,ScaleName="D Kurd 10 (Cyclops)",Count=1,Rate=1500000,Total=1500000},
    new(){UserId=id,UserName="آبتین ترسه",Action=6,ActionTitle="تیون",MaterialName="Stainless Steel",BowlType=2,ScaleName="زیر 2 نت زیر F-G — تعداد نت نامشخص",Count=1,Rate=400000,Total=400000},
    new(){UserId=id,UserName="آبتین ترسه",Action=6,ActionTitle="تیون",MaterialName="VELVET",BowlType=1,ScaleName="روی 11 نت رو",Count=1,Rate=1500000,Total=1500000},
    new(){UserId=id,UserName="آبتین ترسه",Action=7,ActionTitle="فاین تیون",MaterialName="VELVET",ScaleName="D Kurd 15",Count=2,Rate=900000,Total=1800000},
    new(){UserId=Guid.NewGuid(),UserName="عضو دوم",Action=5,ActionTitle="چسب",Count=1,Rate=300000,Total=300000},
    new(){UserId=id,UserName="آبتین ترسه",Action=3,ActionTitle="شیپ",MaterialName="Stainless Steel",BowlType=1,ScaleName="D Kurd-Cyclops سفارشی با توضیحات طولانی",Count=3,Rate=1250000,Total=3750000,IsCustom=true},
    new(){UserId=id,UserName="آبتین ترسه",Action=7,ActionTitle="فاین تیون",MaterialName="متریال با نام طولانی برای بررسی اندازه",ScaleName="D Kurd 18",Count=999,Rate=123456789,Total=123333332211,IsExport=true}
   ]};
  }
  return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(data)});
 }
}
sealed class FixtureJs:IJSRuntime{public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>ValueTask.FromResult(default(T)!);public ValueTask<T> InvokeAsync<T>(string id,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(id,args);}
sealed class FixtureNavigation:NavigationManager{public FixtureNavigation(){Initialize("https://fixture.invalid/","https://fixture.invalid/reports/payroll");}protected override void NavigateToCore(string uri,bool forceLoad){}}
