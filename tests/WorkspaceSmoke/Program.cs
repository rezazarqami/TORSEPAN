using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused").Options;
await using var db=new TORSEPANDbContext(options);
Check(typeof(NotificationsController).GetCustomAttribute<AuthorizeAttribute>() is not null,"inbox requires authentication");
foreach(var name in new[]{"Send","Recipients"})
    Check(typeof(NotificationsController).GetMethod(name)!.GetCustomAttribute<AuthorizeAttribute>()?.Roles=="Administrator,ProductionManager",name+" restricted to manager/admin");
Check(typeof(MyActivityController).GetCustomAttribute<AuthorizeAttribute>() is not null,"personal activity requires authentication");
Check(!typeof(MyActivityController).GetMethod("Get")!.GetParameters().Any(x=>x.Name=="userId"),"personal report exposes no user-selection parameter");
var schema=db.GetService<IMigrator>().GenerateScript("20260829023000_LinkPayrollToAccounting","20260903010000_AddWorkshopMessages");
Check(schema.Contains("CREATE TABLE \"WorkshopMessages\"")&&schema.Contains("CREATE TABLE \"WorkshopMessageReceipts\""),"message migration generates both tables");
Check(schema.Contains("IX_ProductionEvents_UserId_EventDate")&&schema.Contains("IX_WorkshopMessageReceipts_RecipientId_ReadAt"),"personal query and unread indexes included");
var receipt=db.Model.FindEntityType(typeof(WorkshopMessageReceipt))!;
Check(receipt.FindPrimaryKey()!.Properties.Count==2,"receipts unique per message and user");
Check(receipt.GetForeignKeys().Count()==2,"message and recipient foreign keys configured");
var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString())},"test");
var context=new ControllerContext{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(identity)}};
var activity=new MyActivityController(db){ControllerContext=context};
Check(await activity.Get(new DateTime(2026,9,2),new DateTime(2026,9,1)) is BadRequestObjectResult,"reversed range rejected without a database call");
Check(await activity.Get(new DateTime(1,1,1),new DateTime(1,1,1)) is BadRequestObjectResult,"date underflow rejected");
var notifications=new NotificationsController(db){ControllerContext=context};
Check(await notifications.Send(new SendWorkshopMessage(Guid.NewGuid()," ","body",true,null),default) is BadRequestObjectResult,"empty message title rejected");
Check(await notifications.Send(new SendWorkshopMessage(Guid.NewGuid(),"title","body",true,Guid.NewGuid()),default) is BadRequestObjectResult,"ambiguous broadcast recipient rejected");
var exportTune = new ProductionEvent(null, null, Guid.NewGuid(), Guid.NewGuid(),
    TORSEPAN.Domain.Enums.ProductionAction.Tune, TORSEPAN.Domain.Enums.EventResult.Completed,
    null, "Tune completed - export package");
Check(exportTune.ConvertExportTuneToNormalRoute() && exportTune.Description == "Tune completed",
    "returning an export-tuned bowl converts its tune event to the normal route");
var normalTune = new ProductionEvent(null, null, Guid.NewGuid(), Guid.NewGuid(),
    TORSEPAN.Domain.Enums.ProductionAction.Tune, TORSEPAN.Domain.Enums.EventResult.Completed,
    null, "Tune completed");
Check(!normalTune.ConvertExportTuneToNormalRoute() && normalTune.Description == "Tune completed",
    "normal tune events remain unchanged");
Console.WriteLine("Database-backed send/read isolation still requires integration testing against a test PostgreSQL database.");
