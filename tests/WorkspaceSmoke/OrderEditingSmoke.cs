using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Services;

internal static class OrderEditingSmoke
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    private static async Task Reject(Func<Task> work,string message){try{await work();}catch(OrderValidationException){Check(true,message);return;}throw new Exception(message);}
    public static async Task RunAsync()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options;
        await using var db=new TORSEPANDbContext(options);await db.Database.EnsureCreatedAsync();
        var user=new User("edit-test","ویرایش");
        var scale=new Scale("Ordered scale",ScaleUsage.Handpan);
        var another=new Scale("Active custom",ScaleUsage.CustomHandpan);
        var design=new DesignType("Ordered design");var material=new Material("Editing");
        var bowl=new Bowl("EDIT-4",BowlType.Top,true,InstrumentType.Custom,material.Id);
        db.AddRange(user,scale,another,design,material,bowl);await db.SaveChangesAsync();
        var clock=new OrdersSmoke.TestClock(new DateTimeOffset(2026,10,7,8,0,0,TimeSpan.Zero));
        var service=new CustomerOrderService(db,clock);
        var draft=new SaveOrderDraftRequest("Before edit",30,new DateOnly(2026,10,1),
            [new(another.Id,null,2),new(scale.Id,design.Id,5),new(another.Id,null,5)]);
        var id=await service.SaveDraftAsync(null,draft,user.Id,default);await service.FinalizeAsync(id,1,default);
        var order=(await service.GetAsync(default)).Single(x=>x.Id==id);
        var coded=order.Lines[1];var last=order.Lines[2];
        await service.AssignLineCodeAsync(id,coded.Id,4,bowl.ProductionCode,user.Id,default);
        order=(await service.GetAsync(default)).Single(x=>x.Id==id);
        var sent=await db.OrderReminders.SingleAsync(x=>x.OrderId==id&&x.Milestone==1);
        sent.Delivered(clock.GetUtcNow().UtcDateTime);await db.SaveChangesAsync();
        var sentDate=sent.DueAtUtc;
        var edit=new SaveOrderDraftRequest("After edit",45,new DateOnly(2026,9,30),
            [new(scale.Id,design.Id,7,coded.Id),new(another.Id,null,2,last.Id),new(another.Id,null,3)],order.Version);
        await Reject(()=>service.UpdateOrderAsync(id,edit with {Version=order.Version-1},default),"final order editing rejects stale versions");
        await Reject(()=>service.UpdateOrderAsync(id,edit with {Lines=[new(scale.Id,design.Id,3,coded.Id)]},default),"cannot reduce quantity below the last assigned slot");
        await Reject(()=>service.UpdateOrderAsync(id,edit with {Lines=[new(another.Id,null,2,last.Id)]},default),"cannot remove a coded row");
        await Reject(()=>service.UpdateOrderAsync(id,edit with {Lines=[new(another.Id,null,2,Guid.NewGuid())]},default),"cannot import another order's line ID");
        await Reject(()=>service.UpdateOrderAsync(id,edit with {Lines=[new(scale.Id,design.Id,7,coded.Id),new(scale.Id,design.Id,8,coded.Id)]},default),"duplicate retained line IDs are rejected");
        design.Rename("Catalog renamed");design.Deactivate();scale.Deactivate();await db.SaveChangesAsync();
        Check(await service.UpdateOrderAsync(id,edit,default),"final order can add/remove uncoded rows, change counts and dates");
        order=(await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(!order.IsDraft&&order.CustomerName=="After edit"&&order.DurationDays==45&&order.TotalQuantity==12
            &&order.Version==edit.Version+1&&order.Lines.Select(x=>x.Position).SequenceEqual(new[]{1,2,3}),
            "finalized state, totals, contiguous positions and version are correct");
        Check(order.Lines[0].Id==coded.Id&&order.Lines[0].Instruments.Single().Code=="EDIT-4"
            &&order.Lines[0].Instruments.Single().Slot==4&&order.Lines[0].Quantity==7,
            "editing preserves assigned code and stable line identity");
        Check(order.Lines[0].DesignName=="Ordered design"&&order.Lines[0].ScaleName=="Ordered scale",
            "unchanged specifications preserve snapshots even after catalog rename and deactivation");
        Check(order.CreatedAtUtc==TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026,9,30),TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"))
            &&order.DueAtUtc==order.CreatedAtUtc.AddDays(45),"edited registration date and deadline use Tehran calendar");
        Check(sent.SentAtUtc.HasValue&&sent.DueAtUtc==sentDate&&await db.OrderReminders.CountAsync(x=>x.OrderId==id)==5,
            "editing preserves sent reminders and never duplicates registration notice");
        var pending=await db.OrderReminders.SingleAsync(x=>x.OrderId==id&&x.Milestone==4);
        Check(pending.DueAtUtc==order.DueAtUtc&&pending.NextAttemptAtUtc==pending.DueAtUtc,
            "pending reminders follow the edited deadline");
        await Reject(()=>service.UpdateOrderAsync(id,edit,default),"old edit cannot overwrite newer order");
        var reduced=edit with {Version=order.Version,Lines=[new(scale.Id,design.Id,4,coded.Id),new(another.Id,null,1,last.Id)]};
        Check(await service.UpdateOrderAsync(id,reduced,default),"quantity can shrink to the last coded slot and unused added rows can be removed");
        order=(await service.GetAsync(default)).Single(x=>x.Id==id);
        Check(order.TotalQuantity==5&&order.AssignedQuantity==1&&await db.Bowls.AnyAsync(x=>x.Id==bowl.Id),
            "quantity reduction preserves instrument and production record");
        await using var reopened=new TORSEPANDbContext(options);
        var persisted=(await new CustomerOrderService(reopened,clock).GetAsync(default)).Single(x=>x.Id==id);
        Check(persisted.TotalQuantity==5&&persisted.Lines[0].Instruments.Single().Code=="EDIT-4",
            "order edit survives a fresh database context");
    }
}
