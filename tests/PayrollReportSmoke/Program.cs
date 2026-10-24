using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Reporting;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
    (n, _, _) => n == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
await db.Database.EnsureCreatedAsync();
var user = new User("payroll-test", "کاربر آزمایشی");
var material = new Material("Stainless Steel", MaterialCategory.BowlMaterial);
var scale = new Scale("D Kurd", ScaleUsage.All);
db.AddRange(user, material, scale);
await db.SaveChangesAsync();
Handpan Add(string code, int? topNotes, int? bottomNotes, bool custom = false)
{
    var top = new Bowl(code, BowlType.Top, true, InstrumentType.Standard, material.Id);
    var bottom = new Bowl("B-" + code, BowlType.Bottom, true, InstrumentType.Standard, material.Id);
    if (custom) top.SetCustomScalePending();
    top.SetScale(scale.Id); bottom.SetScale(scale.Id);
    if (topNotes.HasValue) top.SetNoteCount(topNotes.Value);
    if (bottomNotes.HasValue) bottom.SetNoteCount(bottomNotes.Value);
    var assembly = new HandpanAssembly(top.Id, bottom.Id);
    var handpan = assembly.CreateHandpan(code);
    handpan.SetScale(scale.Id); handpan.SetNoteCountFromBowls(top, bottom);
    handpan.ChangeStage(ProductionStage.FinishedWarehouse);
    db.AddRange(top, bottom, assembly, handpan,
        new ProductionEvent(handpan.Id, assembly.Id, null, user.Id, ProductionAction.FineTune, EventResult.Completed, null, "test"),
        new ProductionEvent(handpan.Id, assembly.Id, null, user.Id, ProductionAction.Packaging, EventResult.Completed, null, "test"),
        new ProductionEvent(null, null, top.Id, user.Id, ProductionAction.Tune, EventResult.Completed, null, "test"),
        new ProductionEvent(null, null, bottom.Id, user.Id, ProductionAction.Tune, EventResult.Completed, null, "test"));
    return handpan;
}
Add("TEST-92", 9, 2); Add("TEST-93", 9, 3, true); Add("TEST-102", 10, 2);
Add("TEST-UNKNOWN", null, 2); Add("TEST-UNKNOWN-BOTTOM", 9, null);
db.Add(new PayrollRate(ProductionAction.Tune, material.Id, BowlType.Top, null, false, false, 100));
await db.SaveChangesAsync(); db.ChangeTracker.Clear();
var controller = new PayrollController(db, null!, null!, null!, null!);
var result = (OkObjectResult)await controller.Get(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
var c = (PayrollCalculation)result.Value!;
Check(c.UnknownInstruments!.Select(x => x.Code).SequenceEqual(new[] { "TEST-UNKNOWN", "TEST-UNKNOWN-BOTTOM" }), "unknown notes return exact instrument and bowl codes");
Check(c.Lines.All(x => !x.IsCustom), "legacy Custom operations use ordinary classification");
Check(c.Lines.Where(x => x.Action == 6 && x.BowlType == 1).All(x => x.Rate == 100), "legacy Custom uses ordinary payroll rates");
var names = c.Lines.Where(x => x.Action == 7).Select(x => x.ScaleName).ToList();
Check(names.Any(x => x.Contains("(9+2)")) && names.Any(x => x.Contains("(9+3)")) && names.Any(x => x.Contains("(10+2)")), "completed summary distinguishes top and bottom combinations");
Check(c.Lines.Any(x => x.Action == 6 && x.BowlType == 2 && x.ScaleName.Contains("3 نوت")), "bottom note counts remain visible");
PayrollLine Line(string name, int count, bool custom = false) => new(user.Id, "کاربر آزمایشی", 0, 6, "تیون", material.Id, material.Name, 1, Guid.NewGuid(), name, count, 2000000, count * 2000000m, true, custom);
var originals = new[] { Line("D Kurd ۹", 9), Line("D Kurd 9 نوت", 5, true) };
var merged = PayrollReportGrouping.Merge(originals);
Check(merged.Count == 1 && merged[0].Count == 14 && merged[0].Total == 28000000, "9 plus 5 export aliases merge despite legacy IDs and Custom");
Check(PayrollReportGrouping.Merge([Line("2 نوت", 3) with { BowlType = 2 }, Line("3 نوت", 5) with { BowlType = 2 }]).Count == 2, "different bottom counts never merge");
Check(PayrollReportGrouping.Merge(c.Lines).Sum(x => x.Total) == c.Lines.Sum(x => x.Total), "report aggregation preserves exact payable totals");
Check(PayrollReportGrouping.Merge(c.Lines).Where(x => x.Action == 6).Select(x => x.BowlType).SequenceEqual(PayrollReportGrouping.Merge(c.Lines).Where(x => x.Action == 6).Select(x => x.BowlType).OrderBy(x => x)), "all tune top bowls precede bottom bowls");
QuestPDF.Settings.License = LicenseType.Community;
using (var font = typeof(PayrollController).Assembly.GetManifestResourceStream("TORSEPAN.API.Assets.Vazirmatn-Regular.ttf")!) FontManager.RegisterFont(font);
var charts = new ProductionAnalytics(c.From,c.To,0,0,0,0,0,0,0,0,[],[],[],0,0,0,0,[],[],[]);
var bytes = (byte[])typeof(PayrollController).GetMethod("BuildPdf", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [c with { Lines = c.Lines.Concat(originals).ToList() }, charts])!;
var output = Environment.GetEnvironmentVariable("PAYROLL_PDF_PREVIEW");
if (output != null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); await File.WriteAllBytesAsync(output, bytes); }
Check(bytes.Length > 1000, "actual payroll PDF renders with unknown codes and grouped operations");
