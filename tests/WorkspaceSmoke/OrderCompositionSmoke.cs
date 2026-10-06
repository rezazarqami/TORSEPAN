using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Services;

internal static class OrderCompositionSmoke
{
    private static void Check(bool value, string message) { if(!value)throw new Exception(message); Console.WriteLine("PASS " + message); }
    private static async Task Reject(Func<Task> action, string message)
    { try { await action(); } catch(OrderValidationException) { Check(true,message); return; } throw new Exception(message); }
    public static async Task RunAsync()
    {
        if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,(name,_,_)=>name=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
        await using var db = new TORSEPANDbContext(options); await db.Database.EnsureCreatedAsync();
        var user = new User("composition", "کاربر آزمایشی");
        var scale9 = new Scale("D Kurd 9", ScaleUsage.CustomHandpan); var scale12 = new Scale("D Kurd 12", ScaleUsage.CustomHandpan);
        var plain = new DesignType("ساده"); var acid = new DesignType("اسیدکاری"); var milling = new DesignType("فرزکاری"); var inactive = new DesignType("غیرفعال"); inactive.Deactivate();
        var material = new Material("آزمایش"); db.AddRange(user,scale9,scale12,material);await db.SaveChangesAsync();
        var clock = new OrdersSmoke.TestClock(new DateTimeOffset(2026,10,6,10,0,0,TimeSpan.Zero));
        var service = new CustomerOrderService(db,clock);
        SaveOrderDraftRequest Request(params OrderLineRequest[] lines) => new("مشتری چندساز",30,new DateOnly(2026,10,5),lines);
        var omittedDesign = System.Text.Json.JsonSerializer.Deserialize<OrderLineRequest>(
            $$"""{"scaleId":"{{scale9.Id}}","quantity":2}""", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Check(omittedDesign.DesignTypeId is null,"omitted design in API payload defaults to simple");
        var simpleId = await service.SaveDraftAsync(null,Request(omittedDesign),user.Id,default);
        var simpleDraft = (await service.GetAsync(default)).Single(x=>x.Id==simpleId);
        Check(simpleDraft.Lines[0].DesignTypeId is null && simpleDraft.Lines[0].DesignName=="دیزاین ساده" && simpleDraft.TotalQuantity==2,
            "simple draft is saved without any design catalog entries");
        await service.SaveDraftAsync(simpleId,Request(new OrderLineRequest(scale9.Id,null,3)) with {Version=simpleDraft.Version},user.Id,default);
        simpleDraft = (await service.GetAsync(default)).Single(x=>x.Id==simpleId);
        await service.FinalizeAsync(simpleId,simpleDraft.Version,default);
        var simpleOrder = (await service.GetAsync(default)).Single(x=>x.Id==simpleId);
        Check(!simpleOrder.IsDraft && simpleOrder.TotalQuantity==3 && simpleOrder.Lines[0].DesignName=="دیزاین ساده",
            "simple draft remains simple through editing and finalization");
        db.AddRange(plain,acid,milling,inactive);await db.SaveChangesAsync();
        var mixedId = await service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,null,2),new OrderLineRequest(scale12.Id,acid.Id,4)),user.Id,default);
        var mixedDraft = (await service.GetAsync(default)).Single(x=>x.Id==mixedId);
        await service.FinalizeAsync(mixedId,mixedDraft.Version,default);
        Check((await service.GetAsync(default)).Single(x=>x.Id==mixedId).Lines.Select(x=>x.DesignName).SequenceEqual(new[]{"دیزاین ساده","اسیدکاری"}),
            "simple and catalog designs coexist in a finalized multi-line order");
        await Reject(()=>service.SaveDraftAsync(null,Request(),user.Id,default),"empty composition is rejected");
        foreach(var count in new[]{0,-1,10001})await Reject(()=>service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,plain.Id,count)),user.Id,default),"invalid quantity rejected: "+count);
        await Reject(()=>service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,plain.Id,6000),new OrderLineRequest(scale12.Id,acid.Id,5000)),user.Id,default),"total quantity overflow rejected");
        await Reject(()=>service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,Guid.NewGuid(),5)),user.Id,default),"unknown design rejected");
        await Reject(()=>service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,inactive.Id,5)),user.Id,default),"inactive design rejected");
        var id = await service.SaveDraftAsync(null,Request(new OrderLineRequest(scale9.Id,plain.Id,5),new OrderLineRequest(scale9.Id,milling.Id,5),new OrderLineRequest(scale12.Id,acid.Id,5)),user.Id,default);
        var draft = (await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(draft.IsDraft && draft.TotalQuantity==15 && draft.Lines.Count==3 && draft.AssignedQuantity==0,"three specifications and their quantities saved as one draft");
        Check(draft.Reminders.Count==0 && !await db.OrderReminders.AnyAsync(x=>x.OrderId==id),"draft has no deadline or registration reminders");
        await Reject(()=>service.AssignLineCodeAsync(id,draft.Lines[0].Id,1,"ANY",user.Id,default),"draft cannot accept production codes before finalization");
        await Reject(()=>service.SaveDraftAsync(id,Request(new OrderLineRequest(scale9.Id,plain.Id,5)) with {Version=0},user.Id,default),"stale draft update rejected");
        var revised = Request(new OrderLineRequest(scale9.Id,plain.Id,30),new OrderLineRequest(scale12.Id,acid.Id,5)) with {Version=draft.Version};
        await service.SaveDraftAsync(id,revised,user.Id,default);
        draft = (await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(draft.TotalQuantity==35 && draft.Lines.Count==2 && draft.Version==2,"draft replaces old rows atomically and advances version");
        await Reject(()=>service.FinalizeAsync(id,1,default),"stale finalization rejected");
        Check(await service.FinalizeAsync(id,draft.Version,default),"reviewed draft can be finalized");
        var finalized=(await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(!finalized.IsDraft && finalized.Reminders.Count==4 && await db.OrderReminders.CountAsync(x=>x.OrderId==id)==5,"finalization enables four milestones and one registration notice");
        Check(await service.FinalizeAsync(id,draft.Version,default) && await db.OrderReminders.CountAsync(x=>x.OrderId==id)==5,"finalization retry is idempotent");
        await Reject(()=>service.SaveDraftAsync(id,revised,user.Id,default),"finalized quantity and specifications cannot be changed by draft endpoint");
        var line=finalized.Lines[0]; var other=finalized.Lines[1];
        var bowls=Enumerable.Range(1,32).Select(n=>new Bowl("BULK-"+n,BowlType.Top,true,InstrumentType.Custom,material.Id)).ToArray();db.AddRange(bowls);await db.SaveChangesAsync();
        for(var n=1;n<=30;n++)Check(await service.AssignLineCodeAsync(id,line.Id,n,bowls[n-1].ProductionCode,user.Id,default),"register numbered slot "+n);
        await Reject(()=>service.AssignLineCodeAsync(id,line.Id,31,bowls[30].ProductionCode,user.Id,default),"31st code rejected for quantity 30");
        await Reject(()=>service.AssignLineCodeAsync(id,line.Id,0,bowls[30].ProductionCode,user.Id,default),"zero slot rejected");
        await Reject(()=>service.AssignLineCodeAsync(id,other.Id,1,bowls[0].ProductionCode,user.Id,default),"same instrument cannot be repeated in another line of the same order");
        await Reject(()=>service.AssignLineCodeAsync(id,line.Id,2,"bulk-۱",user.Id,default),"Persian digits and aliases cannot bypass duplicate instrument protection");
        await Reject(()=>service.AssignLineCodeAsync(id,line.Id,1,bowls[30].ProductionCode,user.Id,default,""),"stale empty slot cannot overwrite another operator's assigned code");
        Check(await service.AssignLineCodeAsync(id,line.Id,1,bowls[30].ProductionCode,user.Id,default),"occupied numbered slot can be corrected without increasing capacity");
        Check(await service.AssignLineCodeAsync(id,other.Id,1,bowls[0].ProductionCode,user.Id,default),"replaced instrument is available to its correct line");
        var second=await service.CreateAsync(new("سفارش دیگر",scale9.Id,10),user.Id,default);
        await Reject(()=>service.AssignCodeAsync(second,bowls[30].ProductionCode,user.Id,default),"legacy one-code endpoint cannot reuse a multi-order instrument");
        Check(!await service.AssignLineCodeAsync(id,Guid.NewGuid(),1,bowls[31].ProductionCode,user.Id,default),"line belonging to another order returns not-found");
        finalized=(await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(finalized.AssignedQuantity==31 && finalized.Lines[0].Instruments.Count==30 && finalized.Lines[1].Instruments.Count==1,"codes stay under correct design and scale with exact counts");
        bowls[30].ChangeStage(ProductionStage.WaitingForShape);db.ProductionEvents.Add(new ProductionEvent(null,null,bowls[30].Id,user.Id,ProductionAction.Dimple,EventResult.Completed,null,"test"));await db.SaveChangesAsync();
        finalized=(await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(finalized.Lines[0].Instruments[0].ProductionStage=="در انتظار شیپ" && finalized.Lines[0].Instruments[0].CompletedOperations.Contains("دیمپل کاسه رو"),"each instrument has its own live stage and completed operations");
        acid.Rename("نام تازه");scale12.Rename("نام اسکیل تازه");await db.SaveChangesAsync();
        Check((await service.GetAsync(default)).Single(x=>x.Id==id).Lines[1].DesignName=="اسیدکاری","final order preserves design specification after catalog rename");
        var newDesign=new DesignType("دیزاین تازه");db.Add(newDesign);await db.SaveChangesAsync();
        var newDraft=await service.SaveDraftAsync(null,Request(new OrderLineRequest(scale12.Id,newDesign.Id,2)),user.Id,default);
        Check((await service.GetAsync(default)).Single(x=>x.Id==newDraft).Lines[0].DesignName=="دیزاین تازه","new catalog design is accepted without code or deployment changes");
        newDesign.Deactivate();await db.SaveChangesAsync();await Reject(()=>service.FinalizeAsync(newDraft,1,default),"deactivated draft design cannot be finalized silently");
        var reminder=OrderReminderProcessor.BuildMessage(await db.CustomerOrders.SingleAsync(x=>x.Id==id),1,clock.GetUtcNow().UtcDateTime,finalized);
        Check(reminder.Contains("30 ساز") && reminder.Contains("اسیدکاری") && reminder.Contains("BULK-31") && reminder.Length<4096,"multi-order reminder lists specifications and bounded instrument summary");
        var largeOrder=new CustomerOrder(new string('ر',200),scale9.Id,scale9.Name,30,user.Id,clock.GetUtcNow().UtcDateTime);
        for(var n=1;n<=100;n++)largeOrder.Lines.Add(new(largeOrder.Id,n,scale9.Id,new string('س',200),plain.Id,new string('د',200),100));
        var largeMessage=OrderReminderProcessor.BuildMessage(largeOrder,4,clock.GetUtcNow().UtcDateTime);
        Check(largeMessage.Length<4096 && largeMessage.Contains("موعد تحویل") && largeMessage.Contains("زمان باقی‌مانده") && largeMessage.Contains("مهلت این سفارش به پایان رسیده"),"large orders preserve essential deadline information within Telegram message limit");
        var before=await db.Bowls.CountAsync();await service.DeleteAsync(id,default);
        Check(!await db.CustomerOrderLines.AnyAsync(x=>x.OrderId==id) && !await db.OrderInstruments.AnyAsync(x=>x.LineId==line.Id) && before==await db.Bowls.CountAsync(),"order deletion cascades lines and links while preserving production records");
        Check(await service.AssignCodeAsync(second,bowls[30].ProductionCode,user.Id,default),"deleted aggregate releases its code for another order");
        await using var restarted=new TORSEPANDbContext(options);
        Check((await new CustomerOrderService(restarted,clock).GetAsync(default)).Single(x=>x.Id==newDraft).TotalQuantity==2,"saved draft survives a new service and database context");
        await using var pg=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused").Options);
        var migrator=pg.GetService<IMigrator>();var target=pg.Database.GetMigrations().Single(x=>x.EndsWith("AddOrderComposition"));
        var script=migrator.GenerateScript("20261006133310_AddMessagePush",target);
        if(Environment.GetEnvironmentVariable("TORSEPAN_ORDER_MIGRATION_DIR") is { } directory)
        { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory,"up.sql"),script); await File.WriteAllTextAsync(Path.Combine(directory,"down.sql"),migrator.GenerateScript(target,"20261006133310_AddMessagePush")); }
        Check(script.Contains("CREATE TABLE \"CustomerOrderLines\"") && script.Contains("INSERT INTO \"OrderInstruments\"") && !script.Contains("Accounting") && !script.Contains("DROP INDEX"),"migration adds only order composition and backfills legacy codes");
        await OrderCompositionPostgresSmoke.RunIfConfiguredAsync();
        await OrderPreviewFixtures.RenderAsync();
    }
}
