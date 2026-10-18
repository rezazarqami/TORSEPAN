using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

internal static class ProductionNoteCountSmoke
{
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
    public static async Task RunAsync()
    {
        if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name,_,_)=>name=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var db=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite("Data Source=:memory:").Options);
        await db.Database.OpenConnectionAsync();await db.Database.EnsureCreatedAsync();
        var material=new Material("note edit steel");db.Add(material);
        var top=new Bowl("NOTE-TOP",BowlType.Top,true,InstrumentType.Standard,material.Id);
        top.ChangeStage(ProductionStage.WaitingForBake);
        var bottom=new Bowl("NOTE-BOTTOM",BowlType.Bottom,false,InstrumentType.Standard,material.Id);
        db.AddRange(top,bottom);await db.SaveChangesAsync();
        var api=new AdminProductionEditsController(db);
        var initial=await api.Get(top.ProductionCode,default);
        Check(((AdminProductionEditDto)((OkObjectResult)initial.Result!).Value!).Bowls.Single().NoteCount is null,"legacy unknown note count stays unknown on lookup");
        var saved=await api.Update(top.ProductionCode,new(null,[new(top.Id,null,9)],[]),default);
        Check(saved.Result is OkObjectResult && (await db.Bowls.AsNoTracking().SingleAsync(x=>x.Id==top.Id)).NoteCount==9,"bowl code can repair unknown shape note count");
        Check(top.Stage==ProductionStage.WaitingForBake,"note edit preserves current production stage");
        await api.Update(top.ProductionCode,new(null,[new(top.Id,null)],[]),default);
        Check(top.NoteCount==9,"older request without count preserves saved count");
        Check((await api.Update(top.ProductionCode,new(null,[new(top.Id,null,0)],[]),default)).Result is BadRequestObjectResult,"top zero count rejected");
        Check((await api.Update(top.ProductionCode,new(null,[new(top.Id,null,-1)],[]),default)).Result is BadRequestObjectResult,"negative count rejected");
        Check((await api.Update(bottom.ProductionCode,new(null,[new(bottom.Id,null,2)],[]),default)).Result is BadRequestObjectResult,"plain bottom bowl cannot acquire inconsistent note count");
        db.ChangeTracker.Clear();
        var assembly=new HandpanAssembly(top.Id,bottom.Id);db.Add(assembly);
        var handpan=new Handpan(assembly.Id,"NOTE-INSTRUMENT");db.Add(handpan);await db.SaveChangesAsync();
        saved=await api.Update(handpan.SerialNumber,new(null,[new(top.Id,null,11),new(bottom.Id,null,0)],[]),default);
        Check(saved.Result is OkObjectResult && (await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==handpan.Id)).NoteCount==11,"instrument code repairs bowl count and recalculates total with plain bottom zero");
        saved=await api.Update(top.ProductionCode,new(null,[new(top.Id,null,12)],[]),default);
        Check(saved.Result is OkObjectResult && (await db.Handpans.AsNoTracking().SingleAsync(x=>x.Id==handpan.Id)).NoteCount==12,"assembled bowl code also updates instrument total");
        var notedTop=new Bowl("NOTED-TOP",BowlType.Top,true,InstrumentType.Standard,material.Id);
        var notedBottom=new Bowl("NOTED-BOTTOM",BowlType.Bottom,true,InstrumentType.Standard,material.Id);
        var notedAssembly=new HandpanAssembly(notedTop.Id,notedBottom.Id);
        var notedInstrument=new Handpan(notedAssembly.Id,"NOTED-INSTRUMENT");
        db.AddRange(notedTop,notedBottom,notedAssembly,notedInstrument);await db.SaveChangesAsync();
        saved=await api.Update(notedInstrument.SerialNumber,new(null,[new(notedTop.Id,null,10),new(notedBottom.Id,null,3)],[]),default);
        Check(saved.Result is OkObjectResult && notedInstrument.NoteCount==13,"noted bottom count contributes to instrument total");
        Check((await api.Update(notedInstrument.SerialNumber,new(null,[new(notedTop.Id,null,int.MaxValue)],[]),default)).Result is BadRequestObjectResult,"overflowing instrument total is rejected before persistence");
        Check((await db.Bowls.AsNoTracking().SingleAsync(x=>x.Id==notedTop.Id)).NoteCount==10,"invalid batch leaves persisted count unchanged");
        var foreign=new Bowl("FOREIGN",BowlType.Top,true,InstrumentType.Standard,material.Id);db.Add(foreign);await db.SaveChangesAsync();
        Check((await api.Update(handpan.SerialNumber,new(null,[new(foreign.Id,null,8)],[]),default)).Result is BadRequestObjectResult,"foreign bowl count cannot be edited through another instrument");
    }
}
