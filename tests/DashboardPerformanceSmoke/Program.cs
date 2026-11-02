using System.Diagnostics;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

if (OperatingSystem.IsLinux())
    System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
        (name, assembly, path) => name == "sqlite3" ? System.Runtime.InteropServices.NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
var meter=new QueryMeter();
await using var db=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).AddInterceptors(meter).Options);
await db.Database.EnsureCreatedAsync();
var now=DateTime.SpecifyKind(DateTime.UtcNow.AddHours(3.5).Date.AddHours(10).AddHours(-3.5),DateTimeKind.Utc);
var calendar=new PersianCalendar();var local=now.AddHours(3.5);
var month=DateTime.SpecifyKind(calendar.ToDateTime(calendar.GetYear(local),calendar.GetMonth(local),1,0,0,0,0).AddHours(-3.5),DateTimeKind.Utc);
var staff=Enumerable.Range(0,8).Select(i=>new User("user"+i,"همکار "+i)).ToArray();
for(var i=0;i<staff.Length;i++)staff[i].SetDisplayOrder(i);
var material=new Material("Steel");db.Users.AddRange(staff);db.Materials.Add(material);
var bowls=Enumerable.Range(0,1000).Select(i=>new Bowl("B-"+i.ToString("D4"),i%2==0?BowlType.Top:BowlType.Bottom,true,InstrumentType.Standard,material.Id)).ToArray();
for(var i=0;i<bowls.Length;i++)bowls[i].ChangeStage(i<180?(i%3==0?ProductionStage.WaitingForShape:i%3==1?ProductionStage.WaitingForTune:ProductionStage.WaitingForGlue):ProductionStage.Sold);
db.Bowls.AddRange(bowls);
var instruments=new List<Handpan>();
for(var i=0;i<60;i++){
 var assembly=new HandpanAssembly(bowls[200+i*2].Id,bowls[201+i*2].Id);var instrument=assembly.CreateHandpan("HP-"+i.ToString("D3"));
 instrument.ChangeStage(i<20?ProductionStage.WaitingForFinalTune:i<40?ProductionStage.FinishedWarehouse:ProductionStage.Sold);
 db.HandpanAssemblies.Add(assembly);instruments.Add(instrument);
}
await db.SaveChangesAsync();
void Event(Bowl? bowl,Handpan? instrument,ProductionAction action,DateTime date,string description,EventResult result=EventResult.Completed,int user=0){
 var row=new ProductionEvent(instrument?.Id,null,bowl?.Id,staff[user%staff.Length].Id,action,result,null,description);row.ChangeEventDate(date);db.ProductionEvents.Add(row);
}
// Most historical rows belong to completed work and must not be pulled to display current queues.
for(var i=0;i<30000;i++)Event(bowls[400+i%600],null,(ProductionAction)(2+i%3),month.AddDays(-20).AddSeconds(i),"Completed",user:i);
for(var i=0;i<bowls.Length;i++){
 Event(bowls[i],null,ProductionAction.Dimple,month.AddDays(-4),"Dimple completed",user:i);
 Event(bowls[i],null,ProductionAction.Shape,month.AddDays(-3),"Shape completed",user:i+1);
 Event(bowls[i],null,ProductionAction.Tune,month.AddDays(-2),"Tune completed",user:i+2);
}
for(var i=0;i<120;i++)Event(bowls[i],null,i%2==0?ProductionAction.Tune:ProductionAction.Shape,now.AddMinutes(-i),"Completed",user:i);
Event(bowls[0],null,ProductionAction.Tune,now,"NOTE: not an operation");
Event(bowls[0],null,ProductionAction.Tune,now,"failed",EventResult.Failed);
for(var i=20;i<40;i++)Event(null,instruments[i],ProductionAction.Packaging,month.AddHours(2+i),"Packaging completed",user:i);
Event(bowls[202],instruments[1],ProductionAction.Glue,now.AddHours(-1),"Glued with bowl bottom",user:1);
Event(bowls[203],instruments[1],ProductionAction.Glue,now.AddHours(-1),"Glued with bowl top",user:1);
Event(bowls[0],null,ProductionAction.Dimple,DateTime.SpecifyKind(local.Date.AddHours(-3.5).AddTicks(-1),DateTimeKind.Utc),"day boundary",user:0);
await db.SaveChangesAsync();db.ChangeTracker.Clear();
var unit=new UnitOfWork(db,null!,null!,null!,null!,new HandpanRepository(db),new HandpanAssemblyRepository(db),new BowlRepository(db),null!,null!,new ProductionEventRepository(db));
var handler=new GetProductionDashboardQueryHandler(unit);
await handler.Handle(new(),default); // warm JIT/EF metadata before measurement
meter.Reset();var clock=Stopwatch.StartNew();var full=await handler.Handle(new(),default);clock.Stop();
Console.WriteLine($"Full dashboard: {clock.ElapsedMilliseconds} ms, {meter.Commands} SQL commands, {meter.Entities} materialized entities, {JsonSerializer.SerializeToUtf8Bytes(full).Length} response bytes");
Check(full.Queues.Single(x=>x.Stage=="آماده شیپ").Groups.Any(x=>x.UserName!="نامشخص"),"performers from prior months are preserved");
Check(full.Queues.Single(x=>x.Stage=="ورودی انبار این ماه").Codes.Count==20,"monthly warehouse queue remains complete");
Check(full.DailyUserOperations.Where(x=>x.Operation=="چسب").Sum(x=>x.Count)==1,"paired glue events count one instrument");
var json=JsonSerializer.Serialize(full);
if(Environment.GetEnvironmentVariable("TORSEPAN_DASHBOARD_BASELINE") is {} baseline)await File.WriteAllTextAsync(baseline,json);
if(Environment.GetEnvironmentVariable("TORSEPAN_DASHBOARD_COMPARE") is {} compare)Check(json==await File.ReadAllTextAsync(compare),"optimized full response exactly matches the original handler on the same fixture");
meter.Reset();clock.Restart();var queues=await handler.Handle(new(null,DashboardSection.Queues),default);clock.Stop();
Console.WriteLine($"Initial queues: {clock.ElapsedMilliseconds} ms, {meter.Commands} SQL commands, {meter.Entities} entities, {JsonSerializer.SerializeToUtf8Bytes(queues).Length} bytes");
Check(JsonSerializer.Serialize(queues.Queues)==JsonSerializer.Serialize(full.Queues),"initial queues retain exact codes, grouping, age, hall totals and warehouse entries");
Check(JsonSerializer.Serialize(queues.HallStock)==JsonSerializer.Serialize(full.HallStock),"production hall totals stay unchanged");
Check(queues.DailyUserOperations.Count==0&&queues.MonthlyUserOperations.Count==0,"closed statistics tabs are not computed or sent on entry");
Check(meter.Commands==4&&meter.Entities<5000,"entry uses four bounded reads instead of graph/history expansion");
meter.Reset();var daily=await handler.Handle(new(null,DashboardSection.Daily),default);
Check(JsonSerializer.Serialize(daily.DailyUserOperations)==JsonSerializer.Serialize(full.DailyUserOperations),"lazy daily tab exactly matches full report including glue, codes and Tehran boundary");
Check(daily.Queues.Count==0&&daily.MonthlyUserOperations.Count==0&&meter.Commands==3,"changing the calendar day does not reload queues or monthly summary");
meter.Reset();var monthly=await handler.Handle(new(null,DashboardSection.Monthly),default);
Check(JsonSerializer.Serialize(monthly.MonthlyUserOperations)==JsonSerializer.Serialize(full.MonthlyUserOperations),"lazy monthly tab exactly matches full report");
Check(monthly.Queues.Count==0&&monthly.DailyUserOperations.Count==0&&meter.Commands==1,"monthly tab issues only its period query");
var previousMonth=calendar.AddMonths(local,-1);var normalized=await handler.Handle(new(previousMonth,DashboardSection.Daily),default);
Check(JsonSerializer.Serialize(normalized.DailyUserOperations)==JsonSerializer.Serialize(daily.DailyUserOperations),"out-of-month selected dates retain existing normalization");
Console.WriteLine("PASS dashboard fixture");
static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
sealed class QueryMeter:DbCommandInterceptor,IMaterializationInterceptor {
 public int Commands,Entities;public List<string> Sql=[];
 public void Reset(){Commands=Entities=0;Sql.Clear();}
 public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken ct=default){Commands++;Sql.Add(command.CommandText);return ValueTask.FromResult(result);}
 public object InitializedInstance(MaterializationInterceptionData data,object entity){Entities++;return entity;}
}
