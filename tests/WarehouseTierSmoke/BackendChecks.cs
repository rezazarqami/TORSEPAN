using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

static class BackendChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    public static async Task Run()
    {
        if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,(n,_,_)=>n=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
        await using var db=new TORSEPANDbContext(options);await db.Database.EnsureCreatedAsync();
        Check((await db.InstrumentWarehouses.OrderBy(x=>x.Id).Select(x=>x.Name).ToListAsync()).SequenceEqual(new[]{"Economy","Studio","Signature"}),"exactly three stable warehouse tiers are seeded");
        var actor=new User("warehouse-fixture","مدیر آزمایشی");var material=new Material("Ember Steel",MaterialCategory.BowlMaterial);material.SetBowlStock(20,20);var scale=new Scale("D Kurd 9",ScaleUsage.All);db.AddRange(actor,material,scale);await db.SaveChangesAsync();
        Handpan Add(string code,ProductionStage stage){var top=new Bowl(code,BowlType.Top,true,InstrumentType.Standard,material.Id);var bottom=new Bowl(code+"-B",BowlType.Bottom,false,InstrumentType.Standard,material.Id);top.ChangeStage(stage);bottom.ChangeStage(stage);var a=new HandpanAssembly(top.Id,bottom.Id);var h=new Handpan(a.Id,code,scale.Id);h.ChangeStage(stage);db.AddRange(top,bottom,a,h);return h;}
        var stock1=Add("ST-001",ProductionStage.FinishedWarehouse);var stock2=Add("ST-002",ProductionStage.FinishedWarehouse);var pending=Add("ST-QC",ProductionStage.WaitingForQualityControl);var sold=Add("ST-SOLD",ProductionStage.Sold);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        // Multiple event collections must survive split reads without duplicates.
        foreach (var instrument in new[] { stock1, pending })
        {
            var assembly = await db.HandpanAssemblies.SingleAsync(x => x.Id == instrument.AssemblyId);
            for (var i = 0; i < 3; i++)
            {
                db.Add(new ProductionEvent(instrument.Id, null, null, actor.Id, ProductionAction.Tune, EventResult.Completed, null, "fixture"));
                db.Add(new ProductionEvent(null, null, assembly.TopBowlId, actor.Id, ProductionAction.Shape, EventResult.Completed, null, "fixture"));
                db.Add(new ProductionEvent(null, null, assembly.BottomBowlId, actor.Id, ProductionAction.Shape, EventResult.Completed, null, "fixture"));
            }
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var repository = new HandpanRepository(db);
        var activeRead = (await repository.GetAllWithAssemblyAsync()).Single(x => x.Id == pending.Id);
        var stockRead = (await repository.GetWarehouseInventoryAsync()).Single(x => x.Id == stock1.Id);
        foreach (var instrument in new[] { activeRead, stockRead })
            Check(instrument.ProductionEvents.Count == 3 && instrument.Assembly.TopBowl.ProductionEvents.Count == 3 &&
                instrument.Assembly.BottomBowl.ProductionEvents.Count == 3 &&
                instrument.ProductionEvents.All(x => x.User.UserName == actor.UserName),
                "split inventory reads preserve all event collections and performer details");
        Check(!db.ChangeTracker.Entries().Any(), "inventory reads do not retain tracked entity graphs");
        Check(await db.Handpans.AllAsync(x=>x.QualityWarehouseId==1),"existing/default instruments belong to Economy");
        var http=new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.Id.ToString()),new Claim(ClaimTypes.Role,"Administrator")},"fixture"))}};
        var uow=new UnitOfWork(db,new UserRepository(db),new RoleRepository(db),new UserRoleRepository(db),new RefreshTokenRepository(db),new HandpanRepository(db),new HandpanAssemblyRepository(db),new BowlRepository(db),new MaterialRepository(db),new ScaleRepository(db),new ProductionEventRepository(db));
        var qc=new CompleteHandpanQualityControlCommandHandler(uow,new UserContext(http));
        var rejected=await qc.Handle(new("ST-QC",false,"OutOfTune",null,3),default);db.ChangeTracker.Clear();Check(rejected.IsSuccess&&(await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==pending.Id)).QualityWarehouseId==1,"rejected QC leaves warehouse classification unchanged");
        var h=await db.Handpans.SingleAsync(x=>x.Id==pending.Id);h.ChangeStage(ProductionStage.WaitingForQualityControl);foreach(var b in await db.Bowls.Where(x=>x.ProductionCode=="ST-QC"||x.ProductionCode=="ST-QC-B").ToListAsync())b.ChangeStage(ProductionStage.WaitingForQualityControl);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var invalid=await qc.Handle(new("ST-QC",true,null,null,4),default);Check(invalid.IsFailure,"QC refuses unknown warehouse IDs");
        var approved=await qc.Handle(new("ST-QC",true,null,null,3),default);db.ChangeTracker.Clear();var approvedStock=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==pending.Id);Check(approved.IsSuccess&&approvedStock.QualityWarehouseId==3&&approvedStock.Stage==ProductionStage.WaitingForPackaging,"QC assigns Signature before packaging");
        approvedStock.ChangeStage(ProductionStage.FinishedWarehouse);db.Update(approvedStock);await db.SaveChangesAsync();db.ChangeTracker.Clear();Check((await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==pending.Id)).QualityWarehouseId==3,"packaging/warehouse entry preserves QC classification");
        var exportPending=Add("ST-QC-EXPORT",ProductionStage.WaitingForQualityControl);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var badExport=await qc.Handle(new("ST-QC-EXPORT",true,null,null,1,(ExportWarehouseLocation)99),default);Check(badExport.IsFailure,"QC rejects invalid export destination");
        var exportQc=await qc.Handle(new("ST-QC-EXPORT",true,null,null,1,ExportWarehouseLocation.London),default);db.ChangeTracker.Clear();
        var reloaded=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==exportPending.Id);Check(exportQc.IsSuccess&&reloaded.Stage==ProductionStage.WaitingForPackaging&&reloaded.ExportWarehouseLocation==ExportWarehouseLocation.London,"QC export destination survives a new database read without premature stock entry");
        var pack=new CompleteHandpanPackagingCommandHandler(uow,new UserContext(http),new SilentAlerts());
        var exportPack=await pack.Handle(new("ST-QC-EXPORT",Array.Empty<Guid>(),null),default);db.ChangeTracker.Clear();
        Check(exportPack.IsSuccess&&await db.Handpans.AnyAsync(x=>x.Id==exportPending.Id&&x.Stage==ProductionStage.ExportWarehouse&&x.ExportWarehouseLocation==ExportWarehouseLocation.London),"packaging uses persisted QC destination without receiving it from the browser");
        Check(await db.Bowls.Where(x=>x.ProductionCode.StartsWith("ST-QC-EXPORT")).AllAsync(x=>x.Stage==ProductionStage.FinishedWarehouse),"export instrument components do not become duplicate export stock");
        var controller=new InstrumentWarehousesController(db){ControllerContext=new ControllerContext{HttpContext=http.HttpContext}};
        var before=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);var warehouse=await db.InstrumentWarehouses.AsNoTracking().SingleAsync(x=>x.Id==1);var renamed=await controller.Rename(1,new("Entry",warehouse.Version),default);db.ChangeTracker.Clear();Check(renamed is OkObjectResult&&await db.Handpans.AllAsync(x=>x.Id!=stock1.Id||x.QualityWarehouseId==1),"renaming changes labels without moving stock");
        var duplicate=await controller.Rename(2,new(" entry ",(await db.InstrumentWarehouses.AsNoTracking().SingleAsync(x=>x.Id==2)).Version),default);db.ChangeTracker.Clear();Check(duplicate is ConflictObjectResult,"duplicate warehouse names are rejected case-insensitively");
        var blank=await controller.Rename(2,new(" ",(await db.InstrumentWarehouses.AsNoTracking().SingleAsync(x=>x.Id==2)).Version),default);db.ChangeTracker.Clear();Check(blank is BadRequestObjectResult,"empty warehouse names are rejected");
        var staleRename=await controller.Rename(1,new("Wrong",warehouse.Version),default);db.ChangeTracker.Clear();Check(staleRename is ConflictObjectResult,"stale rename cannot overwrite a newer name");
        var first=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);var second=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock2.Id);
        var badBatch=await controller.Transfer(new(2,[new(first.Id,first.OfflineVersion),new(sold.Id,(await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==sold.Id)).OfflineVersion)]),default);db.ChangeTracker.Clear();Check(badBatch is ConflictObjectResult&&await db.Handpans.AnyAsync(x=>x.Id==first.Id&&x.QualityWarehouseId==1),"bulk transfer with sold stock has no partial writes");
        var stale=await controller.Transfer(new(2,[new(first.Id,Guid.NewGuid())]),default);db.ChangeTracker.Clear();Check(stale is ConflictObjectResult,"stale stock version stops transfer");
        var moved=await controller.Transfer(new(2,[new(first.Id,first.OfflineVersion),new(second.Id,second.OfflineVersion)]),default);db.ChangeTracker.Clear();var after=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);Check(moved is OkObjectResult&&await db.Handpans.CountAsync(x=>x.QualityWarehouseId==2)==2,"bulk transfer moves all selected stock to Studio");
        Check(after.Stage==before.Stage&&after.UpdatedAt==before.UpdatedAt&&after.CreatedAt==before.CreatedAt&&(await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==material.Id)).TopBowlQuantity==20,"transfer preserves stages, entry timestamps and material stock");
        Check(await db.ProductionEvents.CountAsync(x=>x.Action==ProductionAction.WarehouseTransfer)==2,"each transfer records an audit event outside payroll actions");
        var moveBack=await controller.Transfer(new(3,[new(after.Id,after.OfflineVersion)]),default);db.ChangeTracker.Clear();Check(moveBack is OkObjectResult&&await db.Handpans.AnyAsync(x=>x.Id==after.Id&&x.QualityWarehouseId==3),"individual transfer can move stock again using refreshed version");
        Check(typeof(InstrumentWarehousesController).GetMethod("Rename")!.GetCustomAttribute<AuthorizeAttribute>()!.Roles=="Administrator","warehouse naming is administrator-only");
        Check(typeof(InstrumentWarehousesController).GetMethod("Transfer")!.GetCustomAttribute<AuthorizeAttribute>()!.Roles!.Contains("QualityControl"),"quality control and stock managers can transfer, without sales-only access");
        var current=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);
        var invalidExport=await controller.Transfer(new(0,[new(current.Id,current.OfflineVersion)],(ExportWarehouseLocation)99),default);db.ChangeTracker.Clear();Check(invalidExport is BadRequestObjectResult,"invalid export destination is rejected");
        var exported=await controller.Transfer(new(0,[new(current.Id,current.OfflineVersion)],ExportWarehouseLocation.Iran),default);db.ChangeTracker.Clear();
        current=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);
        Check(exported is OkObjectResult&&current.Stage==ProductionStage.ExportWarehouse&&current.ExportWarehouseLocation==ExportWarehouseLocation.Iran,"warehouse transfer enters real export inventory at the selected country");
        Check(!(await new HandpanRepository(db).GetWarehouseInventorySummaryAsync()).Any(x=>x.Id==current.Id),"exported instrument leaves domestic inventory");
        var domestic=await controller.Transfer(new(2,[new(current.Id,current.OfflineVersion)]),default);db.ChangeTracker.Clear();
        current=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==stock1.Id);
        Check(domestic is OkObjectResult&&current.Stage==ProductionStage.FinishedWarehouse&&current.ExportWarehouseLocation is null,"return to domestic inventory clears export destination");
        var newExport=Add("EX-EGYPT",ProductionStage.FinishedWarehouse);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var savedExport=await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==newExport.Id);
        Check(await controller.Transfer(new(0,[new(savedExport.Id,savedExport.OfflineVersion)],ExportWarehouseLocation.Egypt),default) is OkObjectResult,"Egypt accepts real instrument transfers");db.ChangeTracker.Clear();
        var exportController=new ExportWarehouseController(db);
        Check(await exportController.ChangeLocation("handpan",newExport.Id,new(ExportWarehouseLocation.Riyadh),default) is NoContentResult,"Riyadh accepts export location changes");db.ChangeTracker.Clear();
        Check(await db.Handpans.AnyAsync(x=>x.Id==newExport.Id&&x.ExportWarehouseLocation==ExportWarehouseLocation.Riyadh&&x.Stage==ProductionStage.ExportWarehouse),"Riyadh destination is persisted without changing export stock status");
        var customerOrder=new CustomerOrder("امید همدانی",scale.Id,scale.Name,7,actor.Id,DateTime.UtcNow);
        var topId=await db.HandpanAssemblies.Where(x=>x.Id==current.AssemblyId).Select(x=>x.TopBowlId).SingleAsync();
        customerOrder.AssignInstrument(current.SerialNumber,topId,current.Id,actor.Id,DateTime.UtcNow);db.Add(customerOrder);await db.SaveChangesAsync();
        var inventory=await new TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory.GetWarehouseInventoryQueryHandler(uow,new TORSEPAN.Infrastructure.Services.OrderCustomerLookup(db)).Handle(new(),default);
        Check(inventory.Single(x=>x.HandpanId==current.Id).OrderCustomerName=="امید همدانی","inventory carries the assigned order customer from persisted orders");
        current.Sell("خریدار",null,null,null,false,actor.Id);db.Update(current);await db.SaveChangesAsync();
        var passport=await new GetBowlForDimpleQueryHandler(uow).Handle(new(current.SerialNumber),default);
        Check(passport.IsSuccess&&passport.Value!.Stage==(int)ProductionStage.Sold,"passport uses sold instrument status even when component bowls still say warehouse");
        await MigrationCheck();
    }
    sealed class SilentAlerts:TORSEPAN.Application.Interfaces.IInventoryAlertService { public Task SendLowStockAsync(string name,string kind,int quantity,int threshold,CancellationToken ct)=>Task.CompletedTask; }
    static async Task MigrationCheck()
    {
        var pgOptions=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused").Options;
        await using var pg=new TORSEPANDbContext(pgOptions);Check(!pg.Database.HasPendingModelChanges(),"warehouse migration snapshot matches PostgreSQL model");var migrations=pg.Database.GetMigrations().ToList();var latest=migrations.Single(x=>x.EndsWith("_AddInstrumentQualityWarehouses"));var previous=migrations[migrations.IndexOf(latest)-1];var sql=pg.GetService<IMigrator>().GenerateScript(previous,latest);
        Check(sql.Contains("DEFAULT 1")&&sql.Contains("Economy")&&sql.Contains("Signature")&&!sql.Contains("DROP TABLE")&&!sql.Contains("DROP COLUMN"),"migration adds Economy default and seed tiers without destructive operations");
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).ConfigureWarnings(x=>x.Ignore(RelationalEventId.PendingModelChangesWarning)).Options;
        await using(var legacy=new LegacyContext(options)){await legacy.Database.EnsureCreatedAsync();var u=new User("legacy","Legacy");var m=new Material("Legacy steel");var t=new Bowl("OLD-T",BowlType.Top,true,InstrumentType.Standard,m.Id);var b=new Bowl("OLD-B",BowlType.Bottom,false,InstrumentType.Standard,m.Id);var a=new HandpanAssembly(t.Id,b.Id);var h=new Handpan(a.Id,"OLD-T");h.ChangeStage(ProductionStage.FinishedWarehouse);legacy.AddRange(u,m,t,b,a,h);await legacy.SaveChangesAsync();}
        await using var current=new TORSEPANDbContext(options);await current.Database.ExecuteSqlRawAsync("CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL PRIMARY KEY,\"ProductVersion\" TEXT NOT NULL)");foreach(var migration in current.Database.GetMigrations().Where(x=>x!=latest))await current.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"__EFMigrationsHistory\" VALUES ({migration},{"10.0.10"})");
        await current.Database.MigrateAsync();Check(await current.Handpans.CountAsync()==1&&await current.Handpans.AllAsync(x=>x.QualityWarehouseId==1)&&await current.InstrumentWarehouses.CountAsync()==3,"real upgrade backfills legacy stock into Economy and preserves existing rows");
    }
    sealed class LegacyContext(DbContextOptions<TORSEPANDbContext> options):TORSEPANDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model){base.OnModelCreating(model);model.Entity<Handpan>().Ignore(x=>x.QualityWarehouse);model.Entity<Handpan>().Ignore(x=>x.QualityWarehouseId);model.Ignore<InstrumentWarehouse>();}
    }
}
