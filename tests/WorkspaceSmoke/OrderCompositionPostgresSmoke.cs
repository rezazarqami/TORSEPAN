using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Services;

internal static class OrderCompositionPostgresSmoke
{
    public static async Task RunIfConfiguredAsync()
    {
        var connection=Environment.GetEnvironmentVariable("ORDER_TEST_DB");
        if(string.IsNullOrWhiteSpace(connection)){Console.WriteLine("SKIP PostgreSQL multi-session concurrency: ORDER_TEST_DB not configured");return;}
        if(new NpgsqlConnectionStringBuilder(connection).Database != "order_composition_test")throw new Exception("ORDER_TEST_DB must use the isolated order_composition_test database");
        var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql(connection).Options;
        await using var db=new TORSEPANDbContext(options);await db.Database.EnsureDeletedAsync();await db.Database.EnsureCreatedAsync();
        var user=new User("concurrent","کاربر آزمایش همزمانی");var scale=new Scale("D Kurd 9",ScaleUsage.CustomHandpan);var design=new DesignType("ساده");var material=new Material("آزمایش");db.AddRange(user,scale,design,material);await db.SaveChangesAsync();
        var bowls=Enumerable.Range(1,33).Select(n=>new Bowl("RACE-"+n,BowlType.Top,true,InstrumentType.Custom,material.Id)).ToArray();db.AddRange(bowls);await db.SaveChangesAsync();
        var service=new CustomerOrderService(db,TimeProvider.System);
        var request=new SaveOrderDraftRequest("همزمان",30,null,[new(scale.Id,design.Id,30)]);
        var id=await service.SaveDraftAsync(null,request,user.Id,default);
        async Task<bool> WithService(Func<CustomerOrderService,Task> work)
        {
            await using var context=new TORSEPANDbContext(options);
            try{await work(new CustomerOrderService(context,TimeProvider.System));return true;}
            catch(OrderValidationException){return false;}
            catch(DbUpdateException ex) when(ex.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation}){return false;}
        }
        var finalization=await Task.WhenAll(WithService(s=>s.FinalizeAsync(id,1,default)),WithService(s=>s.FinalizeAsync(id,1,default)));
        if(finalization.Any(x=>!x)||await db.OrderReminders.CountAsync(x=>x.OrderId==id)!=5)throw new Exception("Concurrent finalization duplicated or lost reminders");
        Console.WriteLine("PASS real PostgreSQL concurrent finalization produces exactly five reminders");
        var line=(await service.GetAsync(default)).Single(x=>x.Id==id).Lines.Single();
        for(var slot=1;slot<=29;slot++)await WithService(s=>s.AssignLineCodeAsync(id,line.Id,slot,bowls[slot-1].ProductionCode,user.Id,default,""));
        var last=await Task.WhenAll(WithService(s=>s.AssignLineCodeAsync(id,line.Id,30,bowls[29].ProductionCode,user.Id,default,"")),WithService(s=>s.AssignLineCodeAsync(id,line.Id,30,bowls[30].ProductionCode,user.Id,default,"")));
        if(last.Count(x=>x)!=1||await db.OrderInstruments.CountAsync(x=>x.LineId==line.Id)!=30)throw new Exception("Concurrent operators overwrote the final empty slot or exceeded capacity");
        if(await WithService(s=>s.AssignLineCodeAsync(id,line.Id,31,bowls[31].ProductionCode,user.Id,default,"")))throw new Exception("Thirty-first code accepted");
        Console.WriteLine("PASS real PostgreSQL row lock and expected-code guard preserve the thirtieth slot under concurrent operators");
        var orderA=await service.SaveDraftAsync(null,request with {Lines=[new(scale.Id,design.Id,1)]},user.Id,default);
        var orderB=await service.SaveDraftAsync(null,request with {Lines=[new(scale.Id,design.Id,1)]},user.Id,default);
        await WithService(s=>s.FinalizeAsync(orderA,1,default));await WithService(s=>s.FinalizeAsync(orderB,1,default));
        var all=await service.GetAsync(default);var lineA=all.Single(x=>x.Id==orderA).Lines.Single().Id;var lineB=all.Single(x=>x.Id==orderB).Lines.Single().Id;
        var duplicate=await Task.WhenAll(WithService(s=>s.AssignLineCodeAsync(orderA,lineA,1,bowls[32].ProductionCode,user.Id,default,"")),WithService(s=>s.AssignLineCodeAsync(orderB,lineB,1,bowls[32].ProductionCode,user.Id,default,"")));
        if(duplicate.Count(x=>x)!=1||await db.OrderInstruments.CountAsync(x=>x.TopBowlId==bowls[32].Id)!=1)throw new Exception("Concurrent orders reused one instrument");
        Console.WriteLine("PASS real PostgreSQL uniqueness prevents cross-order instrument races");
        var draftId=await service.SaveDraftAsync(null,request,user.Id,default);
        var edit=await Task.WhenAll(WithService(s=>s.SaveDraftAsync(draftId,request with {CustomerName="الف",Version=1},user.Id,default)),WithService(s=>s.SaveDraftAsync(draftId,request with {CustomerName="ب",Version=1},user.Id,default)));
        if(edit.Count(x=>x)!=1)throw new Exception("Concurrent draft edit did not reject stale version");
        Console.WriteLine("PASS real PostgreSQL draft version rejects concurrent stale edits");
    }
}
