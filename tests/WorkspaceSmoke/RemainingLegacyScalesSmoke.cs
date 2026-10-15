using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Scales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

internal static class RemainingLegacyScalesSmoke
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
    public static async Task RunAsync()
    {
        if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name,_,_)=>name=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var db=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite("Data Source=:memory:").Options);
        await db.Database.OpenConnectionAsync();await db.Database.EnsureCreatedAsync();
        var material=new Material("test steel");db.Add(material);
        var wanted=new List<Guid>();var bowls=new List<Bowl>();
        var stages=new[]{ProductionStage.WaitingForBake,ProductionStage.WaitingForTune,ProductionStage.WaitingForGlue};
        var next=1;
        Bowl Add(BowlType type,ProductionStage stage,Scale scale){var b=new Bowl("TOP-REVIEW-"+next++,type,true,InstrumentType.Standard,material.Id);b.SetScale(scale.Id);b.ChangeStage(stage);db.Add(b);bowls.Add(b);return b;}
        foreach(var name in new[]{"11 نت رو","۱۲ نوت رو","13 نت رو","14 نت رو"}){
            var scale=new Scale(name,ScaleUsage.All);if(name.StartsWith("11"))scale.Deactivate();db.Add(scale);
            foreach(var stage in stages){var b=Add(BowlType.Top,stage,scale);b.SetNoteCount(11);wanted.Add(b.Id);Add(BowlType.Bottom,stage,scale);}
            Add(BowlType.Top,ProductionStage.WaitingForShape,scale);Add(BowlType.Top,ProductionStage.FinishedWarehouse,scale);
            var rejected=Add(BowlType.Top,ProductionStage.WaitingForBake,scale);rejected.Reject();
        }
        foreach(var name in new[]{"15 نت رو","4 نت زیر","D aegean 21"}){var scale=new Scale(name,ScaleUsage.All);db.Add(scale);Add(BowlType.Top,ProductionStage.WaitingForBake,scale);}
        var assemblyScale=new Scale("11 نوت رو",ScaleUsage.All);db.Add(assemblyScale);
        var assembledTop=Add(BowlType.Top,ProductionStage.WaitingForGlue,assemblyScale);var assembledBottom=Add(BowlType.Bottom,ProductionStage.WaitingForGlue,assemblyScale);
        db.Add(new HandpanAssembly(assembledTop.Id,assembledBottom.Id));await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var before=await db.Bowls.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.ScaleId,x.NoteCount,x.Stage,x.Status,x.OfflineVersion}).ToListAsync();
        var unit=new UnitOfWork(db,null!,null!,null!,null!,null!,null!,new BowlRepository(db),null!,new ScaleRepository(db),null!);
        var report=await new GetLegacyTopBowlScalesQueryHandler(unit).Handle(new(),default);
        Check(report.Count==12&&report.Select(x=>x.BowlId).ToHashSet().SetEquals(wanted),"report contains exactly top 11–14-note bowls at bake/tune/glue, including inactive referenced scales");
        Check(report.All(x=>!string.IsNullOrWhiteSpace(x.ProductionCode)&&!string.IsNullOrWhiteSpace(x.StageName)),"report supplies codes and stage labels for manual editing");
        var after=await db.Bowls.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.ScaleId,x.NoteCount,x.Stage,x.Status,x.OfflineVersion}).ToListAsync();
        Check(before.SequenceEqual(after),"top-bowl report never changes scales, counts, stages or concurrency versions");
        Check(GetLegacyTopBowlScalesQueryHandler.IsLegacyTopScale(" ١٢ نت رو ")&&!GetLegacyTopBowlScalesQueryHandler.IsLegacyTopScale("12 نت زیر"),"top-name matching supports Persian/Arabic digits without matching bottom scales");
        var unknown=new Bowl("UNKNOWN",BowlType.Bottom,true,InstrumentType.Standard,material.Id);
        Check(unknown.FormatScaleName("D aegean 21")=="زیر D aegean 21 — تعداد نت نامشخص","named bottom scale does not turn instrument number 21 into a bowl note count");
        var method=typeof(TORSEPAN.API.Controllers.ScalesController).GetMethod("GetLegacyTopBowls")!;
        Check(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(x=>x.Roles=="Administrator,ProductionManager"),"legacy report endpoint is restricted to scale managers");
    }
}
