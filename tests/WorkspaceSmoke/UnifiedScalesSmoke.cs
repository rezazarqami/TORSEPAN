using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Scales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

internal static class UnifiedScalesSmoke
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? System.Runtime.InteropServices.NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await using (var postgres = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused").Options))
        {
            Check(!postgres.Database.HasPendingModelChanges(), "migration snapshot matches current model");
            var script = postgres.GetService<IMigrator>().GenerateScript("20261013010000_CompactBowlCodeTemplates", "20261015190000_UnifyScalesAndBowlNotes");
            Check(script.Contains("unified_scale_map") && script.Contains("pg_constraint") && script.Contains("NoteCount") && !script.Contains("DELETE FROM"),
                "migration remaps references without deleting historical scale rows");
        }
        var user = new User("unified-scales", "تست");
        var material = new Material("steel");
        var scale = new Scale("ره مینور", ScaleUsage.All);
        var top = new Bowl("UNIFY-TOP", BowlType.Top, true, InstrumentType.Standard, material.Id);
        var bottom = new Bowl("UNIFY-BOTTOM", BowlType.Bottom, true, InstrumentType.Standard, material.Id);
        db.AddRange(user, material, scale, top, bottom); await db.SaveChangesAsync();
        var unit = new UnitOfWork(db, new UserRepository(db), null!, null!, null!, new HandpanRepository(db), null!,
            new BowlRepository(db), new MaterialRepository(db), new ScaleRepository(db), new ProductionEventRepository(db));
        var context = new TestUser(user.Id);
        var dimple = new CompleteBowlDimpleCommandHandler(unit, context);
        db.ChangeTracker.Clear();
        Check((await dimple.Handle(new(top.ProductionCode, OperationDuration.Minutes5, Guid.Empty), default)).IsSuccess,
            "dimple completes without a scale");
        db.ChangeTracker.Clear();
        Check((await dimple.Handle(new(bottom.ProductionCode, OperationDuration.Minutes5, Guid.Empty), default)).IsSuccess,
            "bottom dimple completes without a scale");
        var shape = new CompleteBowlShapeCommandHandler(unit, context);
        db.ChangeTracker.Clear();
        Check(!(await shape.Handle(new(top.ProductionCode, OperationDuration.Minutes5, NoteCount: 9), default)).IsSuccess,
            "top requires scale");
        db.ChangeTracker.Clear();
        Check(!(await shape.Handle(new(top.ProductionCode, OperationDuration.Minutes5, scale.Id), default)).IsSuccess,
            "top requires explicit note count");
        db.ChangeTracker.Clear();
        Check(!(await shape.Handle(new(bottom.ProductionCode, OperationDuration.Minutes5), default)).IsSuccess,
            "bottom requires explicit note count");
        db.ChangeTracker.Clear();
        Check((await shape.Handle(new(top.ProductionCode, OperationDuration.Minutes10, scale.Id, NoteCount: 9), default)).IsSuccess,
            "top accepts shared scale and count");
        db.ChangeTracker.Clear();
        Check((await shape.Handle(new(bottom.ProductionCode, OperationDuration.Minutes10, NoteCount: 3), default)).IsSuccess,
            "bottom accepts count without scale");
        db.ChangeTracker.Clear();
        top = await db.Bowls.SingleAsync(x => x.Id == top.Id);
        bottom = await db.Bowls.SingleAsync(x => x.Id == bottom.Id);
        Check(top.FormatScaleName(scale.Name) == "روی ره مینور 9 نوت" && bottom.FormatScaleName(null) == "زیر بدون اسکیل 3 نوت",
            "bowl labels include side and count");
        db.ChangeTracker.Clear();
        var id = await new CreateScaleCommandHandler(unit).Handle(new(" ره   مینور ", 8), default);
        Check(id == scale.Id && await db.Scales.CountAsync() == 1, "normalized duplicate creation keeps one scale");
        db.ChangeTracker.Clear();
        await new DeactivateScaleCommandHandler(unit).Handle(new(scale.Id, 63), default);
        db.ChangeTracker.Clear();
        var saved = await db.Bowls.Include(x => x.Scale).SingleAsync(x => x.Id == top.Id);
        Check(saved.ScaleId == scale.Id && saved.Scale!.Name == "ره مینور" && saved.NoteCount == 9,
            "deleting a scale preserves old bowl references and note counts");
    }
    sealed class TestUser(Guid id) : IUserContext
    {
        public Guid? UserId => id; public string? UserName => "test"; public string? FullName => "test";
        public bool IsAuthenticated => true; public IReadOnlyCollection<string> Roles => [];
        public bool IsInRole(string role) => false;
    }
}
