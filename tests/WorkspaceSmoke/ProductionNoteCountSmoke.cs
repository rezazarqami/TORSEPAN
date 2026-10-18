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
        var user=new User("date-editor", "Date editor");db.Add(user);
        var operation=new ProductionEvent(null,null,top.Id,user.Id,ProductionAction.Shape,EventResult.Completed,OperationDuration.Over60,"Shape completed");
        var original=new DateTime(2026,10,17,22,15,12,DateTimeKind.Utc).AddTicks(1234560);
        operation.ChangeEventDate(original);db.Add(operation);await db.SaveChangesAsync();
        var desired=new DateTime(2026,10,15,20,40,10,DateTimeKind.Utc);
        saved=await api.Update(top.ProductionCode,new(null,[],[new(operation.Id,user.Id,desired,original)]),default);
        Check(saved.Result is OkObjectResult && (await db.ProductionEvents.AsNoTracking().SingleAsync(x=>x.Id==operation.Id)).EventDate==desired,"event timestamp edit persists through bowl-code endpoint");
        Check(operation.Action==ProductionAction.Shape && operation.Duration==OperationDuration.Over60 && operation.Description=="Shape completed","timestamp edit preserves operation details");
        await api.Update(handpan.SerialNumber,new(null,[],[new(operation.Id,user.Id)]),default);
        Check(operation.EventDate==desired,"requests without date preserve timestamp");
        Check((await api.Update(handpan.SerialNumber,new(null,[],[new(operation.Id,user.Id,desired.AddDays(-1),original)]),default)).Result is ConflictObjectResult,"stale timestamp edits are rejected");
        Check((await api.Update(handpan.SerialNumber,new(null,[],[new(operation.Id,user.Id,DateTime.SpecifyKind(desired,DateTimeKind.Unspecified))]),default)).Result is BadRequestObjectResult,"timezone-free timestamp is rejected");
        var assemblyEvent=new ProductionEvent(null,assembly.Id,null,user.Id,ProductionAction.Glue,EventResult.Completed,null,"assembly");
        var instrumentEvent=new ProductionEvent(handpan.Id,null,null,user.Id,ProductionAction.Sale,EventResult.Completed,null,"sale");
        db.AddRange(assemblyEvent,instrumentEvent);await db.SaveChangesAsync();
        saved=await api.Update(handpan.SerialNumber,new(null,[],[new(assemblyEvent.Id,user.Id,desired),new(instrumentEvent.Id,user.Id,desired)]),default);
        Check(saved.Result is OkObjectResult && assemblyEvent.EventDate==desired && instrumentEvent.EventDate==desired,"dates editable for assembly and instrument operations as well as bowls");
        var local=TORSEPAN.Panel.Services.PersianDateTime.ToIranTime(desired);
        Check(local.Hour==0 && local.Minute==10 && local.Day==16 && TORSEPAN.Panel.Services.PersianDateTime.FromIranTime(local)==desired,"Iran local editing converts across UTC date boundary without drift");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var page=new TORSEPAN.Panel.Components.Pages.AdminProductionEdit();
        var row=new TORSEPAN.Panel.Models.AdminEditableEventDto{Id=operation.Id,EventDate=original};
        typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetField("_record",flags)!.SetValue(page,new TORSEPAN.Panel.Models.AdminProductionEditDto{Events=[row]});
        typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetMethod("LoadDates",flags)!.Invoke(page,null);
        var unchanged=typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetMethod("EditedDate",flags)!.Invoke(page,[row]);
        Check(unchanged is null,"untouched editor preserves original subsecond timestamp");
        var dates=(System.Collections.IDictionary)typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetField("_dates",flags)!.GetValue(page)!;
        var draft=dates[operation.Id]!;
        draft.GetType().GetProperty("Date")!.SetValue(draft,null);
        typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetMethod("RequestSave",flags)!.Invoke(page,null);
        Check(typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetField("_messageError",flags)!.GetValue(page) is true &&
            typeof(TORSEPAN.Panel.Components.Pages.AdminProductionEdit).GetField("_confirming",flags)!.GetValue(page) is false,"incomplete date shows validation feedback before submission");
        var foreign=new Bowl("FOREIGN",BowlType.Top,true,InstrumentType.Standard,material.Id);db.Add(foreign);await db.SaveChangesAsync();
        Check((await api.Update(handpan.SerialNumber,new(null,[new(foreign.Id,null,8)],[]),default)).Result is BadRequestObjectResult,"foreign bowl count cannot be edited through another instrument");
    }
}
