using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Scales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

internal static class ScaleFamilySmoke
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
    public static async Task RunAsync()
    {
        foreach(var (name,family,count) in new[]{("Dkurd12","D Kurd",12),("D Kurd 14","D Kurd",14),("Dkurd 10 cyclops","D Kurd Cyclops",10),("۱۰ نت سایکلوپس","Cyclops",10),("12 نت می آمارا","E Amara",12),("D aegean 21","D aegean",21)}){
            var parsed=ScaleFamilyNames.Parse(name);Check(parsed.FamilyName==family&&parsed.NoteCount==count,"safe scale parsing: "+name);
        }
        Check(ScaleFamilyNames.Parse("F#3").NoteCount is null&&ScaleFamilyNames.Parse("D Kurd 0").NoteCount is null,"pitch registers and invalid zero totals are never inferred as note counts");
        Check(ScaleFamilyNames.Parse("2 نت زیر F-G").FamilyName is null && ScaleFamilyNames.GenericNoteCount("2 نت زیر F-G")==2,
            "note-only legacy bottom labels remain count tiers rather than invented musical families");
        Check(ScaleFamilyNames.Parse("11 نت رو").FamilyName is null,"generic note-only names do not acquire an invented musical family");
        if(OperatingSystem.IsLinux())NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,(name,_,_)=>name=="sqlite3"?NativeLibrary.Load("libsqlite3.so.0"):IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        await using var db=new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
        var steel=new Material("steel");var user=new User("glue-tester","test");var scale=new Scale("D aegean",ScaleUsage.All);var kurd=new Scale("D Kurd",ScaleUsage.All);var tier=new Scale("Dkurd12",ScaleUsage.All);
        db.AddRange(steel,user,scale,kurd,tier);await db.SaveChangesAsync();
        db.Entry(tier).Property(x=>x.CanonicalScaleId).CurrentValue=kurd.Id;db.Entry(tier).Property(x=>x.LegacyNoteCount).CurrentValue=12;await db.SaveChangesAsync();
        Check(ScaleFamilyNames.MatchesPayrollRate(tier,kurd.Id,tier.Id,12)&&!ScaleFamilyNames.MatchesPayrollRate(tier,kurd.Id,tier.Id,14)&&!ScaleFamilyNames.MatchesPayrollRate(tier,scale.Id,tier.Id,12),"historical numbered payroll tiers remain distinct by family and note count");
        Check(ScaleFamilyNames.MatchesPayrollRate(tier,kurd.Id,tier.Id,null,tier.Id),"unknown physical note counts retain their exact historical payroll scale");
        Check(!ScaleFamilyNames.MatchesPayrollRate(kurd,kurd.Id,kurd.Id,12,tier.Id),"historical production cannot silently switch to a new base-family rate");
        var unit=new UnitOfWork(db,new UserRepository(db),null!,null!,null!,new HandpanRepository(db),new HandpanAssemblyRepository(db),new BowlRepository(db),new MaterialRepository(db),new ScaleRepository(db),new ProductionEventRepository(db));
        var existing=await new CreateScaleCommandHandler(unit).Handle(new("Dkurd 15",63),default);Check(existing==kurd.Id,"new numbered catalog submissions resolve to the existing base family");db.ChangeTracker.Clear();
        foreach(var (suffix,topCount,bottomCount,plain,total) in new[]{("18",(int?)10,(int?)8,false,(int?)18),("9",(int?)9,(int?)null,true,(int?)9),("unknown",(int?)10,(int?)null,false,(int?)null)}){
            var top=new Bowl("TOP-"+suffix,BowlType.Top,true,InstrumentType.Standard,steel.Id);top.SetScale(kurd.Id);if(topCount.HasValue)top.SetNoteCount(topCount.Value);top.ChangeStage(ProductionStage.WaitingForGlue);
            var bottom=new Bowl("BOTTOM-"+suffix,BowlType.Bottom,!plain,InstrumentType.Standard,steel.Id);if(bottomCount.HasValue)bottom.SetNoteCount(bottomCount.Value);bottom.ChangeStage(ProductionStage.WaitingForGlue);
            db.AddRange(top,bottom);await db.SaveChangesAsync();db.ChangeTracker.Clear();
            var result=await new CompleteBowlGlueCommandHandler(unit,new TestUser(user.Id)).Handle(new(top.ProductionCode,bottom.ProductionCode,scale.Id),default);
            Check(result.IsSuccess,"glue succeeds for "+suffix);db.ChangeTracker.Clear();
            var h=await db.Handpans.Include(x=>x.Scale).SingleAsync(x=>x.SerialNumber==top.ProductionCode);
            Check(h.NoteCount==total&&h.ScaleId==scale.Id,"glue derives total from the bowls and retains the independently selected instrument family: "+suffix);
            Check(h.FormatScaleName(h.Scale!.Name)==(total.HasValue?"D aegean "+total:"D aegean — تعداد نت نامشخص"),"instrument label exposes computed or unknown total: "+suffix);db.ChangeTracker.Clear();
        }
    }
    sealed class TestUser(Guid id):IUserContext{public Guid? UserId=>id;public string? UserName=>"test";public string? FullName=>"test";public bool IsAuthenticated=>true;public IReadOnlyCollection<string> Roles=>[];public bool IsInRole(string role)=>false;}
}
