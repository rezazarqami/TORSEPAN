using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.API.Offline;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Reporting;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;
using TORSEPAN.Application.Handpans.Queries.GetProductionTimeline;

if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,(n,_,_)=>n=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
await using var db=new TORSEPANDbContext(options);await db.Database.EnsureCreatedAsync();
var actor=new User("legacy-admin","مدیر آزمایشی");var maker=new User("old-maker","سازنده قدیمی");
var material=new Material("Ember Steel",MaterialCategory.BowlMaterial);material.SetBowlStock(20,20);
var scale=new Scale("D Kurd",ScaleUsage.All);var wrongScale=new Scale("Bottom only",ScaleUsage.BottomBowl);
db.AddRange(actor,maker,material,scale,wrongScale);await db.SaveChangesAsync();
var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.Id.ToString()),new Claim(ClaimTypes.Role,"Administrator")},"fixture"))};
var controller=new LegacyStockController(db){ControllerContext=new(){HttpContext=http}};
LegacyStockRequest Request(string code,int? location=null)=>new(Guid.NewGuid(),code,material.Id,scale.Id,9,2,2,location,maker.Id,maker.Id,maker.Id,maker.Id,maker.Id,scale.Id,scale.Id);
var original=Request(" old-۰۰۱ ");var result=await controller.Create(original,default);
Check(result is OkObjectResult,"legacy opening stock saves without a bottom code");
db.ChangeTracker.Clear();var saved=await db.Handpans.Include(x=>x.Assembly).ThenInclude(x=>x.TopBowl).Include(x=>x.Assembly).ThenInclude(x=>x.BottomBowl).SingleAsync();
Check(saved.SerialNumber=="OLD-001"&&saved.Stage==ProductionStage.FinishedWarehouse&&saved.QualityWarehouseId==2&&saved.Status==ProductionStatus.Completed,"normalized top code identifies completed stock in selected domestic warehouse");
Check(saved.NoteCount==11&&saved.Assembly.TopBowl.NoteCount==9&&saved.Assembly.BottomBowl.NoteCount==2,"both note counts and instrument total persist");
Check(LegacyStockRecord.DisplayCode(saved.Assembly.BottomBowl.ProductionCode).Contains("نامشخص"),"unknown bottom code is displayed honestly");
var events=await db.ProductionEvents.Where(x=>x.Action!=ProductionAction.Created).ToListAsync();
Check(events.Count==5&&events.All(x=>x.UserId==maker.Id&&x.IsPayrollExcluded),"five historical makers are preserved and excluded from wages");
Check((await db.Materials.SingleAsync(x=>x.Id==material.Id)).TopBowlQuantity==20&&(await db.Materials.SingleAsync(x=>x.Id==material.Id)).BottomBowlQuantity==20,"opening stock never consumes current raw materials");
var retry=await controller.Create(original with{Code="OLD-001"},default);Check(retry is OkObjectResult&&await db.Handpans.CountAsync()==1,"acknowledgment loss retry returns the same stock without duplicates");
Check(await controller.Create(original with{WarehouseId=3},default) is ConflictObjectResult,"same request identity with changed data is rejected");
Check(await controller.Create(Request("old-001"),default) is ConflictObjectResult,"new request cannot duplicate an existing code case-insensitively");
var counts=(await db.Handpans.CountAsync(),await db.Bowls.CountAsync(),await db.ProductionEvents.CountAsync());
foreach(var bad in new[]{Request("BAD")with{TopNotes=0},Request("BAD")with{BottomNotes=-1},Request("BAD")with{ExportLocation=999},Request("BAD")with{WarehouseId=99},Request("BAD")with{MaterialId=Guid.NewGuid()},Request("BAD")with{ScaleId=wrongScale.Id},Request("BAD")with{DimpleUserId=Guid.NewGuid()},Request("BAD")with{BottomNotes=0,BottomScaleId=scale.Id}})
{Check(await controller.Create(bad,default) is BadRequestObjectResult,"invalid counts, destination or reference is rejected");db.ChangeTracker.Clear();}
Check(counts==(await db.Handpans.CountAsync(),await db.Bowls.CountAsync(),await db.ProductionEvents.CountAsync()),"invalid requests leave no partial bowls, assembly or events");
foreach(var country in new[]{1,2,3,4}){
 var request=Request("EXPORT-"+country,country);Check(await controller.Create(request,default)is OkObjectResult,"export opening stock saves");db.ChangeTracker.Clear();
 var item=await db.Handpans.Include(x=>x.Assembly).ThenInclude(x=>x.TopBowl).Include(x=>x.Assembly).ThenInclude(x=>x.BottomBowl).SingleAsync(x=>x.Id==request.RegistrationId);
 Check(item.Stage==ProductionStage.ExportWarehouse&&(int?)item.ExportWarehouseLocation==country&&item.Assembly.TopBowl.Stage==ProductionStage.FinishedWarehouse&&item.Assembly.BottomBowl.Stage==ProductionStage.FinishedWarehouse,"country persists without duplicate export bowl stock");
}
var exclusion=new PayrollExclusionsController(db){ControllerContext=new(){HttpContext=http}};
Check(await exclusion.Save(new([events[0].Id],false),default)is BadRequestObjectResult,"single payroll restore cannot re-bill historical operations");
Check(await exclusion.SaveBatch(new([new(events[0].Id,false)]),default)is BadRequestObjectResult,"batch payroll restore cannot re-bill historical operations");
try{events[0].SetPayrollExcluded(false,actor.Id);throw new Exception("legacy wage guard missing");}catch(InvalidOperationException){Check(true,"domain payroll guard survives direct caller attempts");}
var uow=new UnitOfWork(db,new UserRepository(db),new RoleRepository(db),new UserRoleRepository(db),new RefreshTokenRepository(db),new HandpanRepository(db),new HandpanAssemblyRepository(db),new BowlRepository(db),new MaterialRepository(db),new ScaleRepository(db),new ProductionEventRepository(db));
var inventory=await new GetWarehouseInventoryQueryHandler(uow).Handle(new(),default);
Check(inventory.Count==1&&inventory.Single().SerialNumber=="OLD-001"&&inventory.Single().BottomBowlCode.Contains("نامشخص"),"ordinary domestic stock search includes legacy instrument, excludes export stock and hides technical bottom identity");
var details=await new GetWarehouseInventoryQueryHandler(uow).Handle(new(saved.Id),default);
Check(details.Single().Operations.Where(x=>x.Operation is "دیمپل" or "شیپ" or "تیون" or "فاین تیون").All(x=>x.PerformedBy==maker.FullName),"ordinary stock detail resolves historical dimple, shape, tune and fine tune makers");
var edit=new AdminProductionEditsController(db);var passport=await edit.Get("old-001",default);
Check(passport.Result is OkObjectResult{Value:AdminProductionEditDto dto}&&dto.Bowls.Count==2&&dto.Events.Any(x=>x.Action==(int)ProductionAction.Design&&x.UserId==maker.Id)&&!dto.Events.Any(x=>x.Description.Contains("IMPORT:")),"existing editor finds full passport by original code with design attribution and readable history");
var snapshot=await PassportBackupSnapshot.LoadAsync(db,default);
Check(snapshot.Records.Count==5&&!snapshot.Records.Any(x=>x.Details.Contains(LegacyStockRecord.UnknownBottomPrefix)||x.BottomCode.Contains(LegacyStockRecord.UnknownBottomPrefix)),"ordinary passport backup contains all imported instruments with honest bottom labels");
var timeline=await new GetProductionTimelineQueryHandler(uow).Handle(new("old-001"),default);
Check(timeline.Operations.Count==6&&!timeline.Operations.Any(x=>(x.Notes ?? "").Contains("IMPORT:")),"ordinary timeline contains original attribution with readable import receipt");
Check(typeof(LegacyStockController).GetCustomAttribute<AuthorizeAttribute>()!.Roles=="Administrator","legacy entry is restricted to administrators");
var payroll = new PayrollController(db, null!, null!, null!, null!);
var calculation = await payroll.Get(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), readyForQc:true);
Check(calculation is OkObjectResult { Value: PayrollCalculation wages } && wages.Lines.Count == 0 && wages.HandpanIds.Count == 0,
    "actual wage calculation excludes historical fine tune from both eligible instruments and payable lines");
var exports = await new ExportWarehouseController(db).Get(default);
Check(exports is OkObjectResult { Value: IEnumerable<ExportWarehouseRow> rows } && rows.Count() == 4 && rows.All(x => x.ItemType == "handpan"),
    "ordinary export stock contains one full instrument per country without duplicate component rows");
// Regression: a legacy instrument entered as plain can acquire corrected design history.
var designA=new DesignType("تصحیح طرح اول");var designB=new DesignType("تصحیح طرح دوم");db.AddRange(designA,designB);await db.SaveChangesAsync();
var plain=Request("89") with{DesignUserId=null};Check(await controller.Create(plain,default) is OkObjectResult,"plain legacy instrument 89 registers without a design worker");db.ChangeTracker.Clear();
var legacy89=await db.Handpans.Include(x=>x.Assembly).SingleAsync(x=>x.SerialNumber=="89");
var addDesign=new AdminProductionEditRequest(scale.Id,[],[],[new(legacy89.Assembly.TopBowlId,null,designA.Id,maker.Id)]);
Check((await edit.Update("89",addDesign,default)).Result is OkObjectResult,"editor adds missing design and performer to legacy instrument 89");db.ChangeTracker.Clear();
var designEvent=await db.ProductionEvents.SingleAsync(x=>x.HandpanId==legacy89.Id&&x.Action==ProductionAction.Design);
Check(designEvent.UserId==maker.Id&&designEvent.IsPayrollExcluded&&designEvent.Description.StartsWith(LegacyStockRecord.Prefix),"corrected legacy design remains excluded from historical wages");
var changeDesign=addDesign with{DesignUpdates=[new(legacy89.Assembly.TopBowlId,designEvent.Id,designB.Id,actor.Id)]};
Check((await edit.Update("89",changeDesign,default)).Result is OkObjectResult,"editor changes existing design type and performer");db.ChangeTracker.Clear();
var corrected=await db.ProductionEvents.SingleAsync(x=>x.Id==designEvent.Id);
Check(corrected.UserId==actor.Id&&corrected.Description.Contains(designB.Id.ToString())&&corrected.IsPayrollExcluded&&await db.ProductionEvents.CountAsync(x=>x.HandpanId==legacy89.Id&&x.Action==ProductionAction.Design)==1,"design correction preserves its event identity and wage exclusion without duplicates");
var read=(AdminProductionEditDto)((OkObjectResult)(await edit.Get("89",default)).Result!).Value!;
Check(read.Events.Single(x=>x.Action==(int)ProductionAction.Design).DesignTypeId==designB.Id,"editor reload exposes saved design selection");
Check((await edit.Update("89",changeDesign with{DesignUpdates=[new(Guid.NewGuid(),null,designA.Id,maker.Id)]},default)).Result is BadRequestObjectResult,"design cannot be assigned to another instrument's bowl");
var exportRows=((IEnumerable<ExportWarehouseRow>)((OkObjectResult)exports).Value!).ToList();
Check(exportRows.All(x=>x.Inventory is not null&&x.Inventory.HandpanId==x.Id&&!string.IsNullOrWhiteSpace(x.Inventory.MaterialName)),"export summary supplies shared card metadata for every destination");
var exportId=exportRows[0].Id;
var exportDetails=await new GetWarehouseInventoryQueryHandler(uow).Handle(new(exportId),default);
Check(exportDetails.Count==1&&exportDetails.Single().Operations.Count==6,"exported instrument detail includes the same production and design facts");
Check(await new TORSEPAN.Infrastructure.Services.ProductionDeletionService(db).DeleteHandpanAsync(exportId,default),"exported instrument can be deleted through existing handpan deletion service");
Check(!await db.Handpans.AnyAsync(x=>x.Id==exportId),"confirmed export deletion removes instrument from stock");
// A cached global workspace token may be stale after unrelated online work.
(await db.Users.SingleAsync(x=>x.Id==actor.Id)).SetRole("Administrator");await db.SaveChangesAsync();
var syncDevice=Guid.NewGuid();db.OfflineDevices.Add(new OfflineDevice{DeviceId=syncDevice,OwnerId=actor.Id,EnrolledUtc=DateTime.UtcNow.AddHours(-1)});await db.SaveChangesAsync();
await using var syncServices=new ServiceCollection().AddSingleton(db).BuildServiceProvider();http.RequestServices=syncServices;
var syncService=new OfflineProductionService(db,null!,new HttpContextAccessor{HttpContext=http});
OfflineOperation Import(string code){var data=JsonSerializer.SerializeToNode(Request(code),new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();data["localTopId"]=Guid.NewGuid();data["localBottomId"]=Guid.NewGuid();data["localAssemblyId"]=Guid.NewGuid();return new(Guid.NewGuid(),syncDevice,"@legacyStock","workspace",actor.Id,DateTime.UtcNow,null,WorkspaceModule:"legacyStock",WorkspaceAction:"save",WorkspaceData:JsonSerializer.SerializeToElement(data),WorkspaceExpectedVersion:"older-global-workspace");}
var syncImport=Import("SYNC-NEW");var syncReceipt=await syncService.ApplyAsync(actor.Id,syncImport,default);
Check(syncReceipt.OperationId==syncImport.OperationId&&await db.Handpans.CountAsync(x=>x.SerialNumber=="SYNC-NEW")==1,"opening stock accepts unrelated workspace changes without overwriting existing instruments");
Check((await syncService.ApplyAsync(actor.Id,syncImport,default)).OperationId==syncImport.OperationId&&await db.Handpans.CountAsync(x=>x.SerialNumber=="SYNC-NEW")==1,"opening stock retry after receipt loss remains idempotent");
try{await syncService.ApplyAsync(actor.Id,Import("SYNC-NEW"),default);throw new Exception("duplicate code accepted");}catch(OfflineProblem e){Check(e.Status==409,"manually entered online code still blocks duplicate offline registration");}
db.ChangeTracker.Clear();
try{await syncService.ApplyAsync(actor.Id,Import("SYNC-DEPENDENCY") with{WorkspacePreviousOperationId=Guid.NewGuid()},default);throw new Exception("missing dependency accepted");}catch(OfflineProblem e){Check(e.Code=="dependency","legacy import still requires acknowledgement of prior queued work");}
try{await syncService.ApplyAsync(actor.Id,Import("SYNC-OTHER") with{WorkspaceModule="warehouses"},default);throw new Exception("stale overwrite accepted");}catch(OfflineProblem e){Check(e.Code=="conflict","other workspace writes still reject stale snapshots");}
try{await syncService.ApplyAsync(actor.Id,Import("SYNC-MISSING") with{WorkspaceExpectedVersion=null},default);throw new Exception("missing base accepted");}catch(OfflineProblem e){Check(e.Code=="conflict","missing workspace basis remains rejected");}
await UiChecks.Run();
Console.WriteLine("Legacy stock smoke passed.");
static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
