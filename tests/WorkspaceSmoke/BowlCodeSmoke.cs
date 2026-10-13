using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application;
using TORSEPAN.Application.Bowls;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;
using TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;

internal static class BowlCodeSmoke
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var materials = new List<Material>();
        var bowls = new List<Bowl>();
        foreach (var letter in new[] { 'S', 'V', 'M', 'F', 'C', 'E' })
        {
            var material = new Material(letter.ToString(), MaterialCategory.BowlMaterial);
            material.SetBowlCodeTemplates($"{letter}S-00000", $"{letter}G-00000"); material.AddBowlStock(10, 10);
            materials.Add(material);
            bowls.Add(new($"{letter}S-00112", BowlType.Top, true, InstrumentType.Standard, material.Id));
            bowls.Add(new($"{letter}G-00145", BowlType.Bottom, false, InstrumentType.Standard, material.Id));
            Check(BowlCodeSequence.Suggest(material.Id, BowlType.Top, material.TopBowlCodeTemplate, bowls).SuggestedCode == $"{letter}10113" &&
                BowlCodeSequence.Suggest(material.Id, BowlType.Bottom, material.BottomBowlCodeTemplate, bowls).SuggestedCode == $"{letter}20146",
                $"{letter}: old top/bottom sequences continue independently in the compact format");
        }
        var steel = materials[0];
        var modern = new Bowl("S10120", BowlType.Top, true, InstrumentType.Standard, steel.Id); bowls.Add(modern);
        var otherMaterial = new Material("collision", MaterialCategory.BowlMaterial);
        bowls.Add(new("S10121", BowlType.Top, true, InstrumentType.Standard, otherMaterial.Id));
        Check(BowlCodeSequence.Suggest(steel.Id, BowlType.Top, "S10000", bowls) is { LastCode: "S10120", SuggestedCode: "S10122" }, "mixed history uses the highest sequence and skips globally occupied codes");
        var large = new Bowl("S19999", BowlType.Top, true, InstrumentType.Standard, steel.Id);
        Check(BowlCodeSequence.Suggest(steel.Id, BowlType.Top, "S10000", [large]).SuggestedCode == "S110000", "sequence grows beyond four digits without restarting");
        Check(ProductionCodeNormalizer.Normalize(" s۱۰۱۱۳ ") == "S10113", "lowercase and Persian digits normalize to the uppercase canonical code");
        db.AddRange(materials); db.Add(otherMaterial); db.AddRange(bowls); await db.SaveChangesAsync();
        var unit = new UnitOfWork(db, new UserRepository(db), null!, null!, null!, new HandpanRepository(db),
            new HandpanAssemblyRepository(db), new BowlRepository(db), new MaterialRepository(db), new ScaleRepository(db), new ProductionEventRepository(db));
        var controller = new BowlsController(null!, null!, unit, null!);
        var result = (OkObjectResult)await controller.SuggestedCode(steel.Id, 1, default);
        var suggestion = JsonSerializer.Deserialize<BowlCodeSuggestion>(JsonSerializer.Serialize(result.Value))!;
        Check(suggestion.SuggestedCode == "S10122", "API suggestion carries forward old and new database records");
        var create = new CreateBowlCommandHandler(unit, null!, new NoUser());
        var created = await create.Handle(new() { ProductionCode = "s10122", MaterialId = steel.Id, BowlType = BowlType.Top, HasNotes = true, InstrumentType = InstrumentType.Standard }, default);
        Check(created.IsSuccess && (await db.Bowls.SingleAsync(x => x.Id == created.Value)).ProductionCode == "S10122", "creation stores uppercase compact codes and consumes bowl stock");
        Check(BowlCodeSequence.Suggest(steel.Id, BowlType.Top, "S10000", await db.Bowls.ToListAsync()).SuggestedCode == "S10123", "next suggestion advances after creating a compact code");
        var duplicate = await create.Handle(new() { ProductionCode = "s10122", MaterialId = steel.Id, BowlType = BowlType.Top }, default);
        Check(duplicate.IsFailure && steel.TopBowlQuantity == 9, "case-only duplicate is rejected without consuming stock");
        var passport = new GetBowlForDimpleQueryHandler(unit);
        Check((await passport.Handle(new("s10122"), default)).Value!.ProductionCode == "S10122", "lowercase bowl search finds the same uppercase passport");
        Check((await passport.Handle(new("ss-00112"), default)).Value!.ProductionCode == "SS-00112", "legacy codes remain searchable and unchanged");
        var bottom = bowls.Single(x => x.ProductionCode == "SG-00145");
        var assembly = new HandpanAssembly(modern.Id, bottom.Id); var handpan = assembly.CreateHandpan(modern.ProductionCode);
        db.AddRange(assembly, handpan); await db.SaveChangesAsync();
        var instrument = await new GetHandpanBySerialNumberQueryHandler(new HandpanRepository(db)).Handle(new("s10120"), default);
        Check(instrument!.SerialNumber == "S10120", "lowercase instrument search returns uppercase serial");
        var lowerLegacy = new Bowl("ss-00131", BowlType.Top, true, InstrumentType.Standard, steel.Id);
        db.Add(lowerLegacy); await db.SaveChangesAsync();
        Check((await passport.Handle(new("SS-00131"), default)).Value!.ProductionCode == "SS-00131" && lowerLegacy.ProductionCode == "ss-00131",
            "historical lowercase records are searchable and displayed uppercase without rewriting their stored code");
        Check(await controller.SuggestedCode(steel.Id, 3, default) is BadRequestObjectResult, "invalid bowl type is rejected before generating a code");
        var templates = new MaterialsController(null!, unit);
        Check(await templates.SetBowlCodeTemplates(steel.Id, new("s10000", "s20000"), default) is NoContentResult && steel.TopBowlCodeTemplate == "S10000", "template endpoint accepts lowercase compact templates and saves uppercase");
        Check(await templates.SetBowlCodeTemplates(materials[1].Id, new("S10000", "S20000"), default) is BadRequestObjectResult, "another material cannot reuse the same letter");
        Check(await templates.SetBowlCodeTemplates(steel.Id, new("S20000", "S10000"), default) is BadRequestObjectResult, "top/bottom type markers cannot be reversed");
        Check(bowls.Any(x => x.ProductionCode == "SS-00112"), "template changes preserve previously assigned codes");
        await using var postgres = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused").Options);
        var script = postgres.GetService<IMigrator>().GenerateScript("20261012175705_AddInstrumentQualityWarehouses", "20261013010000_CompactBowlCodeTemplates");
        Check(script.Contains("'10000'") && script.Contains("'20000'") && !script.Contains("UPDATE \"Bowls\"") && !script.Contains("UPDATE \"Handpans\""), "migration changes material templates only, preserving existing bowl/instrument IDs and codes");
    }
    sealed class NoUser : IUserContext
    {
        public Guid? UserId => null; public string? UserName => null; public string? FullName => null;
        public bool IsAuthenticated => false; public IReadOnlyCollection<string> Roles => [];
        public bool IsInRole(string role) => false;
    }
}
