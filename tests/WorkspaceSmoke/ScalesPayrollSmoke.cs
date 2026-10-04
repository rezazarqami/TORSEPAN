using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Scales;
using TORSEPAN.Application.Sales;
using TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;
using TORSEPAN.Panel.Services;
using TORSEPAN.Panel.Services.Api;
using TORSEPAN.Panel.Services.Auth;

internal static class ScalesPayrollSmoke
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }

    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var user = new User("scales-payroll", "مدیر آزمایشی");
        var material = new Material("Test steel");
        var scale = new Scale("11 notes (9+2)", ScaleUsage.TopBowl | ScaleUsage.BottomBowl | ScaleUsage.Handpan);
        var unrelated = new Scale("Unrelated", ScaleUsage.Handpan);
        db.AddRange(user, material, scale, unrelated);
        await db.SaveChangesAsync();
        var handpans = new List<Handpan>();
        var tops = new List<Bowl>();
        for (var i = 0; i < 3; i++)
        {
            var top = new Bowl("SCALE-TOP-" + i, BowlType.Top, true, InstrumentType.Standard, material.Id);
            var bottom = new Bowl("SCALE-BOTTOM-" + i, BowlType.Bottom, false, InstrumentType.Standard, material.Id);
            top.SetScale(scale.Id); bottom.SetScale(scale.Id);
            var assembly = new HandpanAssembly(top.Id, bottom.Id);
            var handpan = assembly.CreateHandpan(top.ProductionCode);
            handpan.SetScale(i == 2 ? unrelated.Id : scale.Id);
            handpan.ChangeStage(ProductionStage.FinishedWarehouse);
            if (i == 1) handpan.Sell("buyer", null, null, null, false, user.Id);
            db.AddRange(top, bottom, assembly, handpan);
            handpans.Add(handpan); tops.Add(top);
        }
        await db.SaveChangesAsync();
        var unit = new UnitOfWork(db, new UserRepository(db), null!, null!, null!, new HandpanRepository(db), null!,
            new BowlRepository(db), new MaterialRepository(db), new ScaleRepository(db), new ProductionEventRepository(db));
        await new RenameScaleCommandHandler(unit).Handle(new(scale.Id, "12 notes (9+3)"), default);
        db.ChangeTracker.Clear();
        var warehouse = await new GetWarehouseInventoryQueryHandler(unit).Handle(new GetWarehouseInventoryQuery(), default);
        var sales = await new GetSalesQueryHandler(unit).Handle(new GetSalesQuery(), default);
        Check(warehouse.Single(x => x.HandpanId == handpans[0].Id).ScaleName == "12 notes (9+3)"
            && sales.Single(x => x.HandpanId == handpans[1].Id).ScaleName == "12 notes (9+3)",
            "renaming the existing scale updates both pre-existing warehouse and sold instruments");
        Check(warehouse.Single(x => x.HandpanId == handpans[2].Id).ScaleName == "Unrelated", "unrelated instrument scale remains unchanged");
        Check((await db.Bowls.AsNoTracking().Include(x => x.Scale).ToListAsync()).All(x => x.Scale!.Name == "12 notes (9+3)"),
            "all linked top and bottom bowls display the renamed scale");
        try { await new RenameScaleCommandHandler(unit).Handle(new(scale.Id, "Unrelated"), default); throw new Exception("Duplicate rename accepted"); }
        catch (InvalidOperationException) { }
        Check((await db.Scales.AsNoTracking().SingleAsync(x => x.Id == scale.Id)).Name == "12 notes (9+3)", "duplicate rename rejected without changing existing scale");

        var first = new ProductionEvent(null, null, tops[0].Id, user.Id, ProductionAction.Dimple, EventResult.Completed, null, "test");
        var second = new ProductionEvent(null, null, tops[0].Id, user.Id, ProductionAction.Shape, EventResult.Completed, null, "test");
        var invalid = new ProductionEvent(null, null, tops[0].Id, user.Id, ProductionAction.Tune, EventResult.Failed, null, "test");
        second.SetPayrollExcluded(true, user.Id);
        db.AddRange(first, second, invalid); await db.SaveChangesAsync();
        var controller = new PayrollExclusionsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test")) } }
        };
        Check(await controller.SaveBatch(new([new(first.Id, true), new(invalid.Id, true)]), default) is BadRequestObjectResult,
            "invalid batch rejected before any exclusion is applied");
        Check(!(await db.ProductionEvents.AsNoTracking().SingleAsync(x => x.Id == first.Id)).IsPayrollExcluded,
            "invalid batch leaves valid rows untouched");
        Check(await controller.SaveBatch(new([new(first.Id, true), new(first.Id, false)]), default) is BadRequestObjectResult,
            "conflicting duplicate operation rejected");
        Check(await controller.SaveBatch(new([new(first.Id, true), new(second.Id, false)]), default) is NoContentResult,
            "one confirmed batch saves both exclusion and restoration");
        db.ChangeTracker.Clear();
        var savedFirst = await db.ProductionEvents.SingleAsync(x => x.Id == first.Id);
        var savedSecond = await db.ProductionEvents.SingleAsync(x => x.Id == second.Id);
        Check(savedFirst.IsPayrollExcluded && savedFirst.PayrollExcludedByUserId == user.Id && savedFirst.PayrollExcludedAtUtc.HasValue
            && !savedSecond.IsPayrollExcluded && savedSecond.PayrollExcludedAtUtc is null,
            "confirmed exclusions retain audit data and restored operations return to payroll");
        Check((await db.ProductionEvents.Where(x => x.Result == EventResult.Completed && !x.IsPayrollExcluded).Select(x => x.Id).ToListAsync())
            .SequenceEqual([second.Id]), "payroll filter excludes only confirmed operations");
        await VerifyPanelActionsAsync();
    }

    private static async Task VerifyPanelActionsAsync()
    {
        var api = new FixtureApi();
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("https://fixture.invalid/api/") }, new TokenStorage(new FixtureJs()));
        var scales = new Scales();
        SetProperty(scales, "ScaleService", new ScaleService(client));
        Invoke(scales, "BeginEdit", new TORSEPAN.Panel.Models.ScaleDto { Id = api.ScaleId, Name = "11 notes (9+2)", Usage = 4 });
        Check((Guid?)GetField(scales, "_editingId") == api.ScaleId, "edit click opens an in-page scale editor without a browser prompt");
        SetField(scales, "_editingName", "12 notes (9+3)");
        await (Task)Invoke(scales, "SaveEditAsync")!;
        Check(api.ScaleWrites == 1 && GetField(scales, "_editingId") is null, "scale save sends update for original ID and closes editor after success");

        var exclusions = new PayrollExclusions();
        SetProperty(exclusions, "Service", new PayrollExclusionService(client));
        var item = new PayrollExclusionEventDto { Id = Guid.NewGuid(), ActionTitle = "Tune", Performer = "Test", IsPayrollExcluded = false };
        SetField(exclusions, "_result", new PayrollExclusionLookupDto { Code = "TEST-123", Events = [item] });
        Invoke(exclusions, "Change", item, true);
        Check(api.PayrollWrites == 0 && !item.IsPayrollExcluded, "checking a payroll row does not save or change committed data before confirmation");
        Invoke(exclusions, "Change", item, false);
        Check(((Dictionary<Guid, bool>)GetField(exclusions, "_pending")!).Count == 0, "reverting a checkbox cancels its pending change");
        Invoke(exclusions, "Change", item, true);
        api.FailPayroll = true;
        await (Task)Invoke(exclusions, "SaveAsync")!;
        Check(!item.IsPayrollExcluded && ((Dictionary<Guid, bool>)GetField(exclusions, "_pending")!).Count == 1
            && GetField(exclusions, "_success") is null, "failed confirmation retains selections and does not display success");
        api.FailPayroll = false;
        await (Task)Invoke(exclusions, "SaveAsync")!;
        Check(item.IsPayrollExcluded && ((Dictionary<Guid, bool>)GetField(exclusions, "_pending")!).Count == 0
            && GetField(exclusions, "_success") is string, "successful confirmation saves selected operations and displays success");
    }

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static object? Invoke(object target, string name, params object?[] args) => target.GetType().GetMethod(name, Flags)!.Invoke(target, args);
    private static object? GetField(object target, string name) => target.GetType().GetField(name, Flags)!.GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Flags)!.SetValue(target, value);
    private static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name, Flags)!.SetValue(target, value);
    private sealed class FixtureJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string id, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string id, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
    private sealed class FixtureApi : HttpMessageHandler
    {
        public Guid ScaleId { get; } = Guid.NewGuid();
        public int ScaleWrites, PayrollWrites;
        public bool FailPayroll;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Put && request.RequestUri!.AbsolutePath.Contains("/scales/"))
            {
                Check(request.RequestUri.AbsolutePath.EndsWith(ScaleId.ToString()), "rename preserves original scale identity");
                ScaleWrites++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/scales"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new { Id = ScaleId, Name = "12 notes (9+3)", Usage = 4 } }) });
            if (request.Method == HttpMethod.Put && request.RequestUri!.AbsolutePath.EndsWith("/payroll-exclusions/batch"))
            {
                PayrollWrites++;
                return Task.FromResult(new HttpResponseMessage(FailPayroll ? HttpStatusCode.BadRequest : HttpStatusCode.NoContent)
                    { Content = FailPayroll ? new StringContent("save failed") : null });
            }
            throw new Exception("Unexpected fixture request " + request.RequestUri);
        }
    }
}
