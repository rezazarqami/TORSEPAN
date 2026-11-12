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
        Check(top.FormatScaleName(scale.Name) == "روی ره مینور 9 نوت" && bottom.FormatScaleName(null) == "3 نت زیر",
            "bowl labels include side and count");
        var bowlsPage = await new TORSEPAN.Infrastructure.Services.BowlQueryService(db).GetAllAsync(
            new TORSEPAN.Application.Common.Pagination.PageRequest(), null, null, null, null, null, default);
        Check(bowlsPage.Items.Single(x => x.Id == bottom.Id).ScaleName == "3 نت زیر",
            "paginated bowl list uses the same note-only label as the passport");
        db.ChangeTracker.Clear();
        var id = await new CreateScaleCommandHandler(unit).Handle(new(" ره   مینور ", 8), default);
        Check(id == scale.Id && await db.Scales.CountAsync() == 1, "normalized duplicate creation keeps one scale");
        db.ChangeTracker.Clear();
        await new DeactivateScaleCommandHandler(unit).Handle(new(scale.Id, 63), default);
        db.ChangeTracker.Clear();
        var saved = await db.Bowls.Include(x => x.Scale).SingleAsync(x => x.Id == top.Id);
        Check(saved.ScaleId == scale.Id && saved.Scale!.Name == "ره مینور" && saved.NoteCount == 9,
            "deleting a scale preserves old bowl references and note counts");

        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a4f8AAAAASUVORK5CYII=";
        var topPhoto = Convert.FromBase64String(png);
        byte[] bottomPhoto = [4, 5, 6], replacement = [7, 8, 9];
        var photoScale = await new CreateScaleCommandHandler(unit).Handle(new("D Kurd", 63, topPhoto, bottomPhoto), default);
        db.ChangeTracker.Clear();
        await new RenameScaleCommandHandler(unit).Handle(new(photoScale, "D Kurd", BottomPhoto: replacement), default);
        db.ChangeTracker.Clear();
        var photos = await db.ScalePhotos.SingleAsync(x => x.ScaleId == photoScale);
        Check(photos.Top!.SequenceEqual(topPhoto) && photos.Bottom!.SequenceEqual(replacement),
            "scale registration stores both photos and replacing one preserves the other");
        await new RenameScaleCommandHandler(unit).Handle(new(scale.Id, "ره مینور", TopPhoto: topPhoto), default);
        db.ChangeTracker.Clear();
        var oldScalePhotos = await db.ScalePhotos.SingleAsync(x => x.ScaleId == scale.Id);
        Check(oldScalePhotos.Top!.SequenceEqual(topPhoto) && oldScalePhotos.Bottom is null,
            "a scale created without photos can receive one side later");
        var controller = new TORSEPAN.API.Controllers.ScalesController(null!);
        var response = (Microsoft.AspNetCore.Mvc.OkObjectResult)await controller.GetPhotos(scale.Id, db, default);
        var sources = System.Text.Json.JsonSerializer.SerializeToElement(response.Value);
        Check(sources.GetProperty("Top").GetString() == $"data:image/png;base64,{png}" &&
            sources.GetProperty("Bottom").ValueKind == System.Text.Json.JsonValueKind.Null,
            "scale photos endpoint returns the saved image and an empty side independently");
        var invalid = await controller.Create(new("Invalid photo", 63, [1, 2, 3]), default);
        Check(invalid.Result is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,
            "scale photo upload rejects invalid content before saving");

        var playable = new ScalePlayground([new("A2", 50, 49), new("C4", 85, 56, Bottom: true)]);
        var designId = Guid.NewGuid();
        var savedPlayground = await controller.SaveVirtualDesign(designId, new("Custom D", new(playable, BottomPhoto: topPhoto), photoScale), db, default);
        Check(savedPlayground is Microsoft.AspNetCore.Mvc.NoContentResult, "saving a playground accepts both bowl sides and a replacement photo");
        db.ChangeTracker.Clear();
        var playableResponse = (Microsoft.AspNetCore.Mvc.OkObjectResult)await controller.GetVirtualDesign(designId, db, default);
        var playableJson = System.Text.Json.JsonSerializer.SerializeToElement(playableResponse.Value);
        var savedNotes = playableJson.GetProperty("Playground").GetProperty("Notes");
        Check(playableJson.GetProperty("Top").GetString() == $"data:image/png;base64,{png}" &&
            playableJson.GetProperty("Bottom").GetString() == $"data:image/png;base64,{png}" &&
            savedNotes[0].GetProperty("Pitch").GetString() == "A2" &&
            savedNotes[1].GetProperty("Bottom").GetBoolean(),
            "reopening a playground restores positions, sides and both photos");
        var publicPhotos = (Microsoft.AspNetCore.Mvc.OkObjectResult)await controller.GetPhotos(photoScale, db, default);
        var publicJson = System.Text.Json.JsonSerializer.SerializeToElement(publicPhotos.Value);
        Check(!publicJson.TryGetProperty("Playground", out _) && publicJson.GetProperty("Bottom").ValueKind == System.Text.Json.JsonValueKind.Null,
            "the shared scale page returns photos only and workshop edits leave them intact");
        Check(ScalePlayground.SoundBank.Count == 45 && ScalePlayground.SoundBank.First() == "F2" &&
            ScalePlayground.SoundBank.Last() == "C#6" && ScalePlayground.SoundBank.All(ScalePlayground.IsPitch) &&
            !ScalePlayground.IsPitch("E2") && !ScalePlayground.IsPitch("D6"),
            "sound bank covers all 45 semitones from F2 to C-sharp 6");
        var badPlayground = await controller.SaveVirtualDesign(designId, new("Custom D", new(new([new("A2", 101, 49)]))), db, default);
        Check(badPlayground is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,
            "playground rejects notes outside the image");
    }
    sealed class TestUser(Guid id) : IUserContext
    {
        public Guid? UserId => id; public string? UserName => "test"; public string? FullName => "test";
        public bool IsAuthenticated => true; public IReadOnlyCollection<string> Roles => [];
        public bool IsInRole(string role) => false;
    }
}
