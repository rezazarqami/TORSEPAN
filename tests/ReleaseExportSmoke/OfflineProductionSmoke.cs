using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Offline;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

internal static class OfflineProductionSmoke
{
    private sealed class QuietAlerts : IInventoryAlertService {
        public Task SendLowStockAsync(string name,string type,int quantity,int threshold,CancellationToken ct)=>Task.CompletedTask;
    }
    private sealed class WarrantyHttp : HttpMessageHandler {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            Calls++;return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
        await using var db = new TORSEPANDbContext(options); await db.Database.EnsureCreatedAsync();
        var registrar = new User("offline-registrar", "ثبت‌کننده"); var actor = new User("offline-performer", "انجام‌دهنده"); var outsider = new User("outsider", "کاربر");
        var adminRole = new Role("Administrator", "مدیر");
        registrar.UserRoles.Add(new UserRole(registrar.Id, adminRole.Id)); actor.UserRoles.Add(new UserRole(actor.Id,adminRole.Id));
        var steel=new Material("Steel"); var scale=new Scale("D Kurd",ScaleUsage.All);
        var top=new Bowl("855",BowlType.Top,true,InstrumentType.Standard,steel.Id); var bottom=new Bowl("196",BowlType.Bottom,false,InstrumentType.Standard,steel.Id);
        top.ChangeStage(ProductionStage.WaitingForDimple);bottom.ChangeStage(ProductionStage.WaitingForDimple);
        db.AddRange(adminRole,registrar,actor,outsider,steel,scale,top,bottom);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var http=new HttpContextAccessor { HttpContext=new DefaultHttpContext { User=new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier,registrar.Id.ToString()),new Claim(ClaimTypes.Role,"Administrator")],"test")) } };
        var services=new ServiceCollection();services.AddLogging();services.AddSingleton(db);services.AddSingleton<IHttpContextAccessor>(http);
        services.AddSingleton<IUserContext,UserContext>();services.AddSingleton<IBowlRepository,BowlRepository>();services.AddSingleton<IHandpanRepository,HandpanRepository>();
        services.AddSingleton<IHandpanAssemblyRepository,HandpanAssemblyRepository>();services.AddSingleton<IMaterialRepository,MaterialRepository>();
        services.AddSingleton<IScaleRepository,ScaleRepository>();services.AddSingleton<IProductionEventRepository,ProductionEventRepository>();
        services.AddSingleton<IUserRepository,UserRepository>();services.AddSingleton<IRoleRepository,RoleRepository>();
        services.AddSingleton<IUserRoleRepository,UserRoleRepository>();services.AddSingleton<IRefreshTokenRepository,RefreshTokenRepository>();
        services.AddSingleton<IInventoryAlertService,QuietAlerts>();services.AddSingleton<IUnitOfWork,UnitOfWork>();services.AddMediatR(x=>x.RegisterServicesFromAssembly(typeof(CompleteBowlDimpleCommand).Assembly));
        using var provider=services.BuildServiceProvider();var service=new OfflineProductionService(db,provider.GetRequiredService<IMediator>(),http);
        var device=Guid.NewGuid();
        async Task<bool> Allowed(Guid ownerId, Guid phone) => JsonSerializer.SerializeToElement(await service.AccessAsync(ownerId,phone,default)).GetProperty("allowed").GetBoolean();
        Check(await Allowed(registrar.Id,device),"first administrator phone may activate offline");
        await service.EnrollAsync(registrar.Id,device,default);
        Check(await Allowed(registrar.Id,device),"registered administrator phone may open offline");
        var replacementDevice=Guid.NewGuid();
        Check(await Allowed(registrar.Id,replacementDevice),"same administrator can open offline from another installation");
        Check(!await Allowed(Guid.NewGuid(),device),"another account cannot open offline on the registered phone");
        await service.EnrollAsync(registrar.Id,device,default);
        async Task Problem(Func<Task> action,string code){try{await action();throw new Exception("Expected "+code);}catch(OfflineProblem error){Check(error.Code==code,"rejects "+code);db.ChangeTracker.Clear();}}
        var originalEnrollment=await db.OfflineDevices.AsNoTracking().SingleAsync();
        await service.EnrollAsync(registrar.Id,replacementDevice,default);
        var retainedEnrollment=await db.OfflineDevices.AsNoTracking().SingleAsync();
        Check(retainedEnrollment.DeviceId==device && retainedEnrollment.EnrolledUtc==originalEnrollment.EnrolledUtc,"installation change preserves enrollment audit and timestamp");
        Check((await service.SnapshotAsync(registrar.Id,replacementDevice,default)).DeviceId==replacementDevice,"snapshot uses requesting installation identity");
        await Problem(()=>service.EnrollAsync(outsider.Id,replacementDevice,default),"device");
        await Problem(()=>service.SnapshotAsync(registrar.Id,Guid.Empty,default),"invalid");
        await Problem(()=>service.SnapshotAsync(outsider.Id,device,default),"device");
        outsider.SetRole("Tuner"); db.Update(outsider); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var snapshot=await service.SnapshotAsync(registrar.Id,device,default);
        Check(snapshot.Bowls.Count==2&&snapshot.Passports.Records.Count==2&&snapshot.Users.All(x=>x.Id!=Guid.Empty),"snapshot includes stable bowl identities, passports and safe performer projections");
        Check(snapshot.Users.Single(x=>x.Id==outsider.Id).Roles.SequenceEqual(["Tuner"]),"legacy single-role accounts remain available without exposing account credentials");
        var recorded=DateTime.UtcNow.AddMinutes(-1);
        OfflineOperation Operation(string code,string action,string version,Guid? previous=null)=>new(Guid.NewGuid(),device,code,action,actor.Id,recorded,version,previous,2,scale.Id, NoteCount: action == "shape" ? (code == "196" ? 0 : 9) : null);
        var first=Operation("855","dimple",snapshot.Bowls.Single(x=>x.Code=="855").Version);
        var receipt=await service.ApplyAsync(registrar.Id,first,default);
        Check(await db.ProductionEvents.CountAsync()==1&&await db.OfflineReceipts.CountAsync()==1,"offline stage and receipt commit together");
        var event1=await db.ProductionEvents.AsNoTracking().SingleAsync();
        Check(event1.UserId==actor.Id&&event1.EventDate==recorded&&receipt.RegistrarId==registrar.Id,"real event time and performer are preserved separately from registrar audit");
        var retry=await service.ApplyAsync(registrar.Id,first,default);
        Check(retry.OperationId==receipt.OperationId&&await db.ProductionEvents.CountAsync()==1,"retry after lost response returns receipt without duplicate stage or payroll event");
        await Problem(()=>service.ApplyAsync(registrar.Id,first with { Duration=3 },default),"identity");
        await Problem(()=>service.ApplyAsync(registrar.Id,first with { OperationId=Guid.NewGuid() },default),"conflict");
        var shape=Operation("855","shape",first.ExpectedVersion,first.OperationId);await service.ApplyAsync(registrar.Id,shape,default);
        Check((await service.BowlAsync("855",default)).Stage==6,"dependent local stage uses prior receipt rather than obsolete snapshot version");
        var bake=Operation("855","bake",first.ExpectedVersion,shape.OperationId);await service.ApplyAsync(registrar.Id,bake,default);
        var tune=Operation("855","tune",first.ExpectedVersion,bake.OperationId);await service.ApplyAsync(registrar.Id,tune,default);
        var bottomDimple=Operation("196","dimple",snapshot.Bowls.Single(x=>x.Code=="196").Version);await service.ApplyAsync(registrar.Id,bottomDimple,default);
        var bottomShape=Operation("196","shape",bottomDimple.ExpectedVersion,bottomDimple.OperationId);await service.ApplyAsync(registrar.Id,bottomShape,default);
        var bottomBake=Operation("196","bake",bottomDimple.ExpectedVersion,bottomShape.OperationId);await service.ApplyAsync(registrar.Id,bottomBake,default);
        Check((await service.BowlAsync("196",default)).Stage==10,"bottom without notes skips tuning exactly as online workflow does");
        var glue=Operation("855","glue",first.ExpectedVersion,tune.OperationId) with {PairCode="196",PairVersion=bottomDimple.ExpectedVersion,PairPreviousOperationId=bottomBake.OperationId};
        await service.ApplyAsync(registrar.Id,glue,default);
        Check(await db.Handpans.CountAsync()==1&&await db.HandpanAssemblies.CountAsync()==1,"offline glue creates one assembly and instrument from two validated bowls");
        Check((await db.Handpans.AsNoTracking().SingleAsync()).CreatedAt==recorded&&(await db.HandpanAssemblies.AsNoTracking().SingleAsync()).AssemblyDate==recorded,"offline instrument and assembly preserve real creation time");
        var release=Operation("196","release",bottomDimple.ExpectedVersion,glue.OperationId);await service.ApplyAsync(registrar.Id,release,default);
        var final=Operation("855","finalTune",first.ExpectedVersion,release.OperationId);await service.ApplyAsync(registrar.Id,final,default);
        var qc=Operation("855","qc",first.ExpectedVersion,final.OperationId);await service.ApplyAsync(registrar.Id,qc,default);
        Check((await service.BowlAsync("855",default)).Stage==16&&(await service.BowlAsync("196",default)).Stage==16,"mixed top/bottom dependency chain reaches packaging after QC");
        await Problem(async()=>await service.ApplyAsync(registrar.Id,Operation("855","unknown",(await service.BowlAsync("855",default)).Version),default),"unsupported");
        var raw=new Material("Offline steel",MaterialCategory.BowlMaterial);raw.SetBowlStock(3,3);
        var pack=new Material("مثلثی",MaterialCategory.Other,6);
        var design=new DesignType("اسیدی");db.AddRange(raw,pack,design);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var versions=new Dictionary<string,string>{{raw.Id.ToString(),(await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==raw.Id)).OfflineVersion.ToString()}};
        var create=Operation("NEW-00001","create","new") with {MaterialId=raw.Id,BowlType=2,HasNotes=false,MaterialVersions=versions};
        await service.ApplyAsync(registrar.Id,create,default);await service.ApplyAsync(registrar.Id,create,default);
        Check((await service.BowlAsync(create.Code,default)).Stage==6 && (await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==raw.Id)).BottomBowlQuantity==2,"new offline bowl consumes stock exactly once and starts in correct stage");
        var create2=create with {OperationId=Guid.NewGuid(),Code="NEW-00002",BowlType=1,MaterialPreviousOperations=new(){{raw.Id.ToString(),create.OperationId}}};
        await service.ApplyAsync(registrar.Id,create2,default);
        var localEdit=Operation(create2.Code,"edit","new",create2.OperationId) with {Edit=new(null,[],[]),EditScales=new(){{create2.Code,scale.Id}}};
        await service.ApplyAsync(registrar.Id,localEdit,default);
        Check((await service.BowlAsync(create2.Code,default)).ScaleId==scale.Id,"scale correction resolves a locally created bowl by code before snapshot refresh");
        var note=Operation("855","note","old",qc.OperationId) with {Details="یادداشت آفلاین",IsInstrumentNote=true};
        await service.ApplyAsync(registrar.Id,note,default);
        var note2=note with {OperationId=Guid.NewGuid(),PreviousOperationId=note.OperationId};await service.ApplyAsync(registrar.Id,note2,default);
        Check(await db.ProductionEvents.CountAsync(x=>x.Description.Contains("یادداشت آفلاین"))==2,"two distinct offline notes are not lost to online time-based deduplication");
        var designOp=Operation("855","design","old",note2.OperationId) with {DesignTypeId=design.Id};await service.ApplyAsync(registrar.Id,designOp,default);
        byte[] image=[255,216,255,217];
        var photo=Operation("855","photo","old",designOp.OperationId) with {Image=Convert.ToBase64String(image),Thumbnail=Convert.ToBase64String(image),ImageHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image))};
        await service.ApplyAsync(registrar.Id,photo,default);await service.ApplyAsync(registrar.Id,photo,default);
        Check(await db.HandpanPhotos.CountAsync()==1 && (await db.HandpanPhotos.AsNoTracking().SingleAsync()).CreatedAt==recorded,"photo upload is idempotent and preserves offline capture time");
        Check(!(await db.OfflineReceipts.AsNoTracking().SingleAsync(x=>x.OperationId==photo.OperationId)).PayloadJson.Contains(photo.Image!),"receipt does not duplicate the binary photo payload");
        var pv=(await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==pack.Id)).OfflineVersion.ToString();
        var packaging=Operation("855","packaging","old",photo.OperationId) with {MaterialIds=[pack.Id],MaterialVersions=new(){{pack.Id.ToString(),pv}}};
        await service.ApplyAsync(registrar.Id,packaging,default);await service.ApplyAsync(registrar.Id,packaging,default);
        Check((await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==pack.Id)).Quantity==4 && (await service.BowlAsync("855",default)).Stage==18,"packaging consumes two triangular items once and moves instrument to warehouse");
        var sale=Operation("855","sell","old",packaging.OperationId) with {BuyerName="خریدار آزمایشی",BuyerPhoneNumber="09120000000",Price=500,ReceivedAmount=200};
        await service.ApplyAsync(registrar.Id,sale,default);await service.ApplyAsync(registrar.Id,sale,default);
        var sold=await db.Handpans.AsNoTracking().SingleAsync();
        Check(sold.SoldAt==recorded && sold.WarrantyActivatedAt==recorded && (await service.BowlAsync("196",default)).Stage==20 && await db.AccountingDocuments.CountAsync()==1,"sale records one accounting document, real sale date, warranty and sold status on both local identities");
        Check((await db.OfflineReceipts.SingleAsync(x=>x.OperationId==sale.OperationId)).WarrantyPending,"offline sale commits a durable remote warranty retry marker");
        var warrantyHttp=new WarrantyHttp();var warrantyClient=new TORSEPAN.API.Services.GuaranteeServiceClient(new HttpClient(warrantyHttp){BaseAddress=new Uri("https://warranty.invalid/")},new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        await OfflineWarrantySyncWorker.SyncAsync(db,warrantyClient,default);await OfflineWarrantySyncWorker.SyncAsync(db,warrantyClient,default);
        Check(warrantyHttp.Calls==1 && !(await db.OfflineReceipts.SingleAsync(x=>x.OperationId==sale.OperationId)).WarrantyPending,"remote warranty activation is deferred until committed sale and not repeated after acknowledgement");
        var alertCalls=0;
        using(var alertBatch=new TORSEPAN.Application.Materials.InventoryAlertBatch()){
            Check(TORSEPAN.Application.Materials.InventoryAlertBatch.TryQueue(()=>{alertCalls++;return Task.CompletedTask;}) && alertCalls==0,"low-stock notifications stay buffered before offline transaction commit");
            await alertBatch.FlushAsync();
        }
        using(var discarded=new TORSEPAN.Application.Materials.InventoryAlertBatch())TORSEPAN.Application.Materials.InventoryAlertBatch.TryQueue(()=>{alertCalls++;return Task.CompletedTask;});
        Check(alertCalls==1,"failed offline work drops buffered notification rather than sending a false stock alert");
        var stock=Operation("@"+raw.Id,"stock","stock") with {MaterialId=raw.Id,TopQuantity=5,BottomQuantity=6,SetAbsolute=true,MaterialPreviousOperations=new(){{raw.Id.ToString(),create2.OperationId}}};
        await service.ApplyAsync(registrar.Id,stock,default);await service.ApplyAsync(registrar.Id,stock,default);
        Check((await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==raw.Id)).TopBowlQuantity==5,"stock adjustment follows material receipt chain independent of bowl chain");
        await Problem(()=>service.ApplyAsync(registrar.Id,create with {OperationId=Guid.NewGuid(),Code="STALE-00001"},default),"stock-conflict");
        var exportTune=Operation(create2.Code,"tuneExport",(await service.BowlAsync(create2.Code,default)).Version);
        var exportBowl=await db.Bowls.SingleAsync(x=>x.ProductionCode==create2.Code);exportBowl.ChangeStage(ProductionStage.WaitingForTune);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        exportTune=exportTune with {ExpectedVersion=(await service.BowlAsync(create2.Code,default)).Version};await service.ApplyAsync(registrar.Id,exportTune,default);
        var exportPack=Operation(create2.Code,"exportPackaging","old",exportTune.OperationId) with {ExportLocation=1};await service.ApplyAsync(registrar.Id,exportPack,default);
        var ship=Operation(create2.Code,"ship","old",exportPack.OperationId) with {BuyerName="صادرات",Destination="ترکیه",ShippingMethod="زمینی"};await service.ApplyAsync(registrar.Id,ship,default);await service.ApplyAsync(registrar.Id,ship,default);
        Check((await service.BowlAsync(create2.Code,default)).Stage==20,"offline export tune, packaging and shipment complete with receipt dependencies");
        snapshot=await service.SnapshotAsync(registrar.Id,device,default);
        Check(snapshot.Passports.Records.Any(x=>x.Code=="855"&&x.Kind=="ساز"),"refreshed snapshot joins paired bowl histories into instrument passport");
        var loose=new Bowl("SS-0090",BowlType.Top,true,InstrumentType.Standard,steel.Id);loose.ChangeStage(ProductionStage.WaitingForShape);db.Add(loose);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var baseline=await service.BowlAsync(loose.ProductionCode,default);
        var invalid=Operation(loose.ProductionCode,"shape",baseline.Version) with { StretchUserId=outsider.Id };
        var eventsBefore=await db.ProductionEvents.CountAsync();
        await Problem(()=>service.ApplyAsync(registrar.Id,invalid,default),"INVALID_REQUEST");
        Check((await service.BowlAsync(loose.ProductionCode,default)).Stage==4&&await db.ProductionEvents.CountAsync()==eventsBefore&&!await db.OfflineReceipts.AnyAsync(x=>x.OperationId==invalid.OperationId),"validation failure leaves no partial production change or receipt");
        db.ProductionEvents.Add(new ProductionEvent(null,null,loose.Id,registrar.Id,ProductionAction.Shape,EventResult.Completed,null,"NOTE:changed online"));await db.SaveChangesAsync();db.ChangeTracker.Clear();
        await Problem(()=>service.ApplyAsync(registrar.Id,Operation(loose.ProductionCode,"shape",baseline.Version),default),"conflict");
        baseline=await service.BowlAsync(loose.ProductionCode,default);
        await Problem(()=>service.ApplyAsync(registrar.Id,Operation(loose.ProductionCode,"shape",baseline.Version) with {PerformerId=outsider.Id},default),"performer");
        await Problem(()=>service.ApplyAsync(registrar.Id,Operation(loose.ProductionCode,"shape",baseline.Version) with {Duration=200},default),"duration");
        await Problem(()=>service.ApplyAsync(registrar.Id,Operation(loose.ProductionCode,"shape",baseline.Version) with {OccurredUtc=DateTime.UtcNow.AddDays(1)},default),"clock");
        await using(var other=new TORSEPANDbContext(options)){
            var stale=await other.Bowls.AsNoTracking().SingleAsync(x=>x.Id==loose.Id);
            var fresh=await db.Bowls.SingleAsync(x=>x.Id==loose.Id);fresh.ChangeStage(ProductionStage.WaitingForBake);await db.SaveChangesAsync();db.ChangeTracker.Clear();
            stale.ChangeStage(ProductionStage.WaitingForTune);other.Update(stale);
            try{await other.SaveChangesAsync();throw new Exception("stale update accepted");}catch(DbUpdateConcurrencyException){Check(true,"detached online writer cannot overwrite newer production state");}
        }
        var rejected = new Bowl("REJECTED",BowlType.Top,true,InstrumentType.Standard,steel.Id);
        rejected.ChangeStage(ProductionStage.WaitingForShape);rejected.Reject();db.Add(rejected);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var rejectedView = await service.BowlAsync(rejected.ProductionCode,default);
        Check(rejectedView.IsRejected,"rejected bowls remain read-only even if their old stage is still a waiting stage");
        await Problem(()=>service.ApplyAsync(registrar.Id,Operation(rejected.ProductionCode,"shape",rejectedView.Version),default),"stage");
        Check(typeof(OfflineController).GetCustomAttribute<AuthorizeAttribute>()?.Roles=="Administrator"&&typeof(OfflineController).GetMethods().All(x=>x.GetCustomAttribute<AllowAnonymousAttribute>() is null),"offline enrollment, snapshot and sync are administrator-only authenticated endpoints");
        var pgOptions=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused").Options;
        await using var pg=new TORSEPANDbContext(pgOptions);
        var sql=pg.GetService<IMigrator>().GenerateScript("20261006185746_AddOrderComposition");
        Check(sql.Contains("OfflineDevices")&&sql.Contains("OfflineReceipts")&&sql.Contains("OfflineVersion")&&!sql.Contains("DROP TABLE"),"additive PostgreSQL migration includes singleton device, durable receipts and concurrency tokens");
        Check(!pg.Database.HasPendingModelChanges(),"migration snapshot matches current PostgreSQL model");
    }
}
