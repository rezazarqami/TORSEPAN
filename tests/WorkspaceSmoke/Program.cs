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
using TORSEPAN.Application.Sales;
using TORSEPAN.Application.Materials;
using TORSEPAN.Application.Common.Reporting;

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
var saleJson=ExportSaleMetadata.Encode("خریدار",Guid.NewGuid(),"آلمان","air",true);
var saleData=ExportSaleMetadata.Decode(saleJson);
Check(saleData is {BuyerName:"خریدار",Destination:"آلمان",ShippingMethod:"air",IsSettled:true},"export shipment details round-trip safely");
var stockJson=MaterialStockMetadata.Encode(Guid.NewGuid(),"استیل","top",12,40,"ورود");
Check(MaterialStockMetadata.Decode(stockJson) is {Delta:12,Balance:40,StockKind:"top"},"material stock movement round-trips safely");
Check(typeof(ManagementReportsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles=="Administrator,ProductionManager","management reports restricted to managers");
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var exportPage=File.ReadAllText(Path.Combine(root,"TORSEPAN.Panel/Components/Pages/ExportWarehouse.razor"));
Check(!exportPage.Contains("InvokeAsync<bool>(\"confirm\"")&&exportPage.Contains("ShipExportBowlsAsync"),"export shipping uses themed bulk confirmation");
var warehouseCss=File.ReadAllText(Path.Combine(root,"TORSEPAN.Panel/Components/Pages/WarehouseList.razor.css"));
var salesCss=File.ReadAllText(Path.Combine(root,"TORSEPAN.Panel/Components/Pages/Sales.razor.css"));
Check(warehouseCss.Contains("flex-direction:column")&&warehouseCss.Contains("min-width:calc(100vw - 24px)"),"warehouse details fit a mobile viewport");
Check(salesCss.Contains("flex-direction:column")&&salesCss.Contains("min-width:calc(100vw - 24px)"),"sales details fit a mobile viewport");
var persianMonths=PersianMonthCalendar.LastMonths(new DateTime(2026,9,10),6);
Check(persianMonths.Select(x=>x.Label).SequenceEqual(new[]{"1405/01","1405/02","1405/03","1405/04","1405/05","1405/06"}),"report trend uses six consecutive Persian months");
Check(persianMonths.Zip(persianMonths.Skip(1)).All(x=>x.First.LocalEnd==x.Second.LocalStart),"Persian report month boundaries are contiguous");
var repeatedBowl=Guid.NewGuid();var earlierBowl=Guid.NewGuid();
var uniqueTrendSource=new List<(Guid Id,DateTime EventDate)>
{
    (repeatedBowl,persianMonths[^1].UtcStart.AddDays(1)),
    (repeatedBowl,persianMonths[^1].UtcStart.AddDays(2)),
    (earlierBowl,persianMonths[^2].UtcStart.AddDays(1))
};
var uniqueTrendMethod=typeof(ManagementReportsController).GetMethod("BuildUniqueTrend",BindingFlags.Static|BindingFlags.NonPublic)!;
var uniqueTrend=(List<TrendPoint>)uniqueTrendMethod.Invoke(null,[uniqueTrendSource,new DateTime(2026,9,10)])!;
Check(uniqueTrend.Sum(x=>x.Count)==2&&uniqueTrend[^1].Count==1,"production trend counts each bowl only once");
var reportsPage=File.ReadAllText(Path.Combine(root,"TORSEPAN.Panel/Components/Pages/Reports.razor"));
Check(reportsPage.Contains("روند ماهانه کاسه‌های ساخته‌شده")&&reportsPage.Contains("روند ماهانه سازهای واردشده به انبار"),"production report separates bowl and warehouse instrument trends");
var pdfPreviews=ReportPdfPreviewFixtures.Build();
Check(pdfPreviews.Count==3&&pdfPreviews.All(x=>x.Value.Length>5000),"all management report PDFs render with complete visual layouts");
var previewDirectory=Environment.GetEnvironmentVariable("TORSEPAN_PDF_PREVIEW_DIR");
if(!string.IsNullOrWhiteSpace(previewDirectory)){Directory.CreateDirectory(previewDirectory);foreach(var preview in pdfPreviews)await File.WriteAllBytesAsync(Path.Combine(previewDirectory,preview.Key),preview.Value);}
Console.WriteLine("Database-backed send/read isolation still requires integration testing against a test PostgreSQL database.");
