using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Panel.Components.Shared;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Auth;
using static TORSEPAN.API.Controllers.WorkPlansController;

static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
static ClaimsPrincipal Identity(Guid id, string role) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Role,role)],"test"));
static WorkPlanDayDto Day(IActionResult result) => JsonSerializer.Deserialize<WorkPlanDayDto>(JsonSerializer.Serialize(((OkObjectResult)result).Value))!;
var day = new DateTime(2026,10,28);
using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var foad = new User("foad","فواد"); foad.SetRole("Dimpler");
var shaper = new User("shaper","شیپ‌کار"); shaper.SetRole("Shaper");
var hall = new User("hall","سالنکار"); hall.SetRole("Workshop");
var manager = new User("manager","مدیر تولید"); manager.SetRole("ProductionManager");
db.Users.AddRange(foad,shaper,hall,manager); await db.SaveChangesAsync();
var controller = new WorkPlansController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User=Identity(manager.Id,"ProductionManager") } } };
var empty = Day(await controller.Get(day,false));
Check(empty.Users.Count==3 && empty.Users.All(x=>x.UserId!=manager.Id),"manager excluded; all other staff included");
Check(empty.Users.Single(x=>x.UserId==hall.Id).Options.Count==0,"hall staff use notes only");
Check(Options(["Dimpler","Shaper","Dimpler"]).Count==4,"multiple roles produce distinct top/bottom tasks");
SaveRow Row(Guid id, int top, int bottom, Guid? revision=null) => new(id,revision,[new("2:1","ignored client label",top),new("2:2","ignored",bottom)],"برنامه روزانه");
Check(await controller.Save(new(day.AddDays(-7),false,[Row(foad.Id,8,2)]),default) is OkObjectResult,"daily plan accepts retrospective start date");
Check(await controller.Save(new(day.AddDays(-7),false,[new(hall.Id,null,[],"نظافت و آماده‌سازی سالن")]),default) is OkObjectResult,"notes-only plan persists");
var topBowl = new Bowl("TOP",BowlType.Top,true,InstrumentType.Standard,Guid.NewGuid());
var bottomBowl = new Bowl("BOTTOM",BowlType.Bottom,false,InstrumentType.Standard,Guid.NewGuid());
db.Bowls.AddRange(topBowl,bottomBowl);
void Event(Bowl bowl, DateTime time, EventResult result=EventResult.Completed, string description="Dimple completed", Guid? user=null)
{
    var e = new ProductionEvent(null,null,bowl.Id,user??foad.Id,ProductionAction.Dimple,result,null,description);
    e.ChangeEventDate(DateTime.SpecifyKind(time,DateTimeKind.Utc)); db.ProductionEvents.Add(e);
}
var start=day.AddHours(-3.5);
for(var n=0;n<9;n++) Event(topBowl,start.AddHours(n));
Event(bottomBowl,start.AddHours(10));
Event(topBowl,start.AddTicks(-1)); Event(topBowl,start.AddDays(1));
Event(topBowl,start.AddHours(1),EventResult.Failed); Event(topBowl,start.AddHours(1),description:"NOTE: ignored");
Event(topBowl,start.AddHours(1),description:"LEGACY_STOCK: ignored"); Event(topBowl,start.AddHours(1),user:shaper.Id);
await db.SaveChangesAsync();
var report = Day(await controller.Get(day,false)); var person=report.Users.Single(x=>x.UserId==foad.Id);
Check(person.Tasks.Single(x=>x.Key=="2:1") is { Target:8,Actual:9,Passed:true },"9 of 8 top dimples passes; Tehran day boundary and excluded events respected");
Check(person.Tasks.Single(x=>x.Key=="2:2") is { Target:2,Actual:1,Passed:false },"1 of 2 bottom dimples fails independently");
Check(person.Tasks[0].Label=="دیمپل کاسه رو","server owns operation labels");
Check(!Day(await controller.Get(day.AddDays(-8),false)).Users.Single(x=>x.UserId==foad.Id).HasPlan,"no plan before effective start");
Check(await controller.Save(new(day,true,[Row(foad.Id,10,1,person.Revision)]),default) is OkObjectResult,"one-day exception saved");
Check(Day(await controller.Get(day,false)).Users.Single(x=>x.UserId==foad.Id) is { IsException:true,Tasks:var exceptionTasks } && exceptionTasks[0].Target==10,"exception overrides same day");
Check(Day(await controller.Get(day.AddDays(1),false)).Users.Single(x=>x.UserId==foad.Id).Tasks[0].Target==8,"next day reverts to recurring plan");
Check(Day(await controller.Get(day,true)).Users.Single(x=>x.UserId==foad.Id).Tasks[0].Target==8,"recurring editor ignores exception");
Check(await controller.Save(new(day,true,[Row(foad.Id,5,1,person.Revision)]),default) is ConflictObjectResult,"stale revisions rejected");
Check(await controller.Save(new(day,false,[new(foad.Id,person.Revision,[new("3:1","invalid",5)],"")]),default) is BadRequestObjectResult,"role-incompatible tasks rejected");
Check(await controller.Save(new(day,false,[Row(manager.Id,1,1)]),default) is BadRequestObjectResult,"production manager cannot receive plan");
controller.HttpContext.User=Identity(foad.Id,"Dimpler");
var mine = Day(await controller.Mine(day)); Check(mine.Users.Count==1 && mine.Users[0].UserId==foad.Id,"mine returns authenticated user's plan only");
controller.HttpContext.User=Identity(manager.Id,"ProductionManager");
var oldRevision=Day(await controller.Get(day.AddDays(2),true)).Users.Single(x=>x.UserId==foad.Id).Revision;
Check(await controller.Save(new(day.AddDays(2),false,[Row(foad.Id,12,3,oldRevision)]),default) is OkObjectResult,"future dated daily revision saved");
Check(Day(await controller.Get(day.AddDays(1),false)).Users.Single(x=>x.UserId==foad.Id).Tasks[0].Target==8 && Day(await controller.Get(day.AddDays(2),false)).Users.Single(x=>x.UserId==foad.Id).Tasks[0].Target==12,"later revision preserves historical daily plan");

await using var authServices = new ServiceCollection().AddLogging().AddAuthorizationCore().BuildServiceProvider();
var policyProvider=authServices.GetRequiredService<IAuthorizationPolicyProvider>(); var auth=authServices.GetRequiredService<IAuthorizationService>();
foreach(var method in new[]{"Get","Save","Mine"})
{
    var attributes=typeof(WorkPlansController).GetCustomAttributes<AuthorizeAttribute>().Cast<IAuthorizeData>().Concat(typeof(WorkPlansController).GetMethod(method)!.GetCustomAttributes<AuthorizeAttribute>()).ToArray();
    var policy=(await AuthorizationPolicy.CombineAsync(policyProvider,attributes))!;
    foreach(var role in new[]{"Administrator","ProductionManager","Dimpler","Workshop","SalesAdmin",""})
    {
        var user=role==""?new ClaimsPrincipal(new ClaimsIdentity()):Identity(foad.Id,role);
        Check((await auth.AuthorizeAsync(user,null,policy)).Succeeded==(role!=""&&(method=="Mine"||role is "Administrator" or "ProductionManager")), $"{method} access for {role}");
    }
}
using(var postgres = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=localhost;Database=design;Username=design").Options))
    Check(!postgres.Database.HasPendingModelChanges(),"migration snapshot matches PostgreSQL model");

// Render the real shared component using an isolated API fixture.
var services = new ServiceCollection().AddLogging();
services.AddSingleton<IJSRuntime,NullJs>(); services.AddScoped<TokenStorage>();
services.AddSingleton(new HttpClient(new FixtureHandler(report)){BaseAddress=new Uri("https://fixture.invalid/api/")}); services.AddScoped<ApiClient>();
await using var sp=services.BuildServiceProvider(); await using var renderer=new HtmlRenderer(sp,sp.GetRequiredService<ILoggerFactory>());
foreach(var editing in new[]{false,true})
{
    var html = await renderer.Dispatcher.InvokeAsync(async()=>WebUtility.HtmlDecode((await renderer.RenderComponentAsync<PlanPreview>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Editing",editing}}))).ToHtmlString()));
    Check(html.Contains("فواد") && (editing? html.Contains("type=\"number\"") && html.Contains("تأیید برنامهٔ روزانه"):html.Contains("✓ تکمیل")&&html.Contains("✕ تکمیل نشده")),editing?"real editor renders targets, notes and dated confirmation":"real report renders per-task success/failure");
    if(editing) await renderer.Dispatcher.InvokeAsync(async()=> {
        var instance=PlanPreview.Instance!;
        var field=typeof(WorkPlanBoard).GetField("_data",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var data=(WorkPlanDayDto)field.GetValue(instance)!;
        var edited=data.Users.Single(x=>x.UserId==foad.Id); edited.Tasks[0].Target=17; edited.Notes="حفظ ویرایش";
        await (Task)typeof(WorkPlanBoard).GetMethod("ChangeEffectiveDate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(instance,[day.AddDays(-7)])!;
        var moved=((WorkPlanDayDto)field.GetValue(instance)!).Users.Single(x=>x.UserId==foad.Id);
        Check(moved.Tasks[0].Target==17 && moved.Notes=="حفظ ویرایش","confirmation date change preserves edited targets and notes");
    });
    if(Environment.GetEnvironmentVariable("WORK_PLAN_PREVIEW_DIR") is string output) { Directory.CreateDirectory(output); await File.WriteAllTextAsync(Path.Combine(output,editing?"editor.html":"report.html"),html); }
}
if(Environment.GetEnvironmentVariable("WORK_PLAN_PREVIEW_DIR") is string previewOutput)
{
    await renderer.Dispatcher.InvokeAsync(async()=> {
        var editorPage=await renderer.RenderComponentAsync<TORSEPAN.Panel.Components.Pages.WorkPlans>(ParameterView.Empty);
        await File.WriteAllTextAsync(Path.Combine(previewOutput,"editor-page.html"),editorPage.ToHtmlString());
        var downloads=await renderer.RenderComponentAsync<TORSEPAN.Panel.Components.Pages.AppDownloads>(ParameterView.Empty);
        await File.WriteAllTextAsync(Path.Combine(previewOutput,"downloads.html"),downloads.ToHtmlString());
    });
}
Console.WriteLine("Work plans smoke passed.");
sealed class NullJs:IJSRuntime
{
 public ValueTask<T> InvokeAsync<T>(string identifier,object?[]? args)=>ValueTask.FromResult(default(T)!);
 public ValueTask<T> InvokeAsync<T>(string identifier,CancellationToken ct,object?[]? args)=>InvokeAsync<T>(identifier,args);
}
sealed class FixtureHandler(WorkPlanDayDto day):HttpMessageHandler
{
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(day)});
}

sealed class PlanPreview:WorkPlanBoard
{
 public static PlanPreview? Instance;
 protected override async Task OnInitializedAsync() { await base.OnInitializedAsync(); Instance=this; }
}
