using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Panel.Components.Pages;
using TORSEPAN.Panel.Models;

internal static class ExportWorkflowSmoke
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    public static async Task RunAsync()
    {
        if (OperatingSystem.IsLinux()) NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_sqlite3).Assembly,
            (name, _, _) => name == "sqlite3" ? NativeLibrary.Load("libsqlite3.so.0") : IntPtr.Zero);
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var user = new User("export-test", "مدیر"); user.SetRole("ProductionManager");
        var material = new Material("Test steel"); db.AddRange(user, material); await db.SaveChangesAsync();
        var top = new Bowl("EXPORT-TOP", BowlType.Top, true, InstrumentType.Standard, material.Id);
        var bottom = new Bowl("EXPORT-BOTTOM", BowlType.Bottom, false, InstrumentType.Standard, material.Id);
        var bowl = new Bowl("EXPORT-LOOSE", BowlType.Top, true, InstrumentType.Standard, material.Id);
        bowl.ChangeStage(ProductionStage.WaitingForGlue);
        var assembly = new HandpanAssembly(top.Id, bottom.Id); var handpan = assembly.CreateHandpan(top.ProductionCode);
        handpan.MoveToExportWarehouse(ExportWarehouseLocation.Iran);
        var tune = new ProductionEvent(null, null, bowl.Id, user.Id, ProductionAction.Tune, EventResult.Completed, null, "Tune completed");
        db.AddRange(top, bottom, bowl, assembly, handpan, tune); await db.SaveChangesAsync();
        var unit = new UnitOfWork(db, new UserRepository(db), null!, null!, null!, new HandpanRepository(db), null!,
            new BowlRepository(db), new MaterialRepository(db), new ScaleRepository(db), new ProductionEventRepository(db));
        db.ChangeTracker.Clear();
        var routing = new SendGlueBowlToExportCommandHandler(unit);
        Check((await routing.Handle(new(bowl.ProductionCode), default)).IsSuccess && (await db.Bowls.AsNoTracking().SingleAsync(x=>x.Id==bowl.Id)).Stage == ProductionStage.WaitingForExportPackaging,
            "ready-for-glue bowl follows the existing export-packaging path");
        bowl = await db.Bowls.SingleAsync(x=>x.Id==bowl.Id);
        tune = await db.ProductionEvents.SingleAsync(x=>x.Id==tune.Id);
        handpan = await db.Handpans.SingleAsync(x=>x.Id==handpan.Id);
        user = await db.Users.SingleAsync(x=>x.Id==user.Id);
        Check(tune.Description == "Tune completed - export package" && tune.UserId == user.Id && await db.ProductionEvents.CountAsync() == 1,
            "route correction preserves tuner and avoids duplicate payroll events");
        Check((await routing.Handle(new(bowl.ProductionCode), default)).IsFailure, "repeat route correction is rejected");
        bowl.MoveToExportWarehouse(ExportWarehouseLocation.Iran); await db.SaveChangesAsync();
        var controller = new ExportWarehouseController(db) { ControllerContext = new() { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test")) } } };
        ExportItemsShipmentRequest Shipment(params ExportShipmentItem[] items) => new(items, "buyer", null, "Germany", "air", true);
        Check(await controller.Ship(Shipment(new ExportShipmentItem(bowl.Id, "bowl"), new ExportShipmentItem(Guid.NewGuid(), "handpan")), default) is ConflictObjectResult && bowl.Stage == ProductionStage.ExportWarehouse,
            "unavailable selected item rejects the whole batch before changing stock");
        Check(await controller.Ship(Shipment(new ExportShipmentItem(bowl.Id, "bowl"), new ExportShipmentItem(handpan.Id, "handpan")), default) is NoContentResult,
            "bowls and instruments ship in one transaction");
        Check(bowl.Stage == ProductionStage.Sold && handpan.Stage == ProductionStage.Sold && handpan.IsExportSale == true && handpan.SoldByUserId == user.Id &&
            await db.ProductionEvents.CountAsync(x => x.Action == ProductionAction.Sale) == 2,
            "shipment preserves sale details and creates one sale event per item");
        Check(await controller.Ship(Shipment(new ExportShipmentItem(bowl.Id, "bowl")), default) is ConflictObjectResult, "shipped item cannot be shipped again");
        Check(await controller.Ship(Shipment(), default) is BadRequestObjectResult, "empty selection is rejected");

        using var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        async Task<TokenValidatedContext> Validate(string role)
        {
            var http = new DefaultHttpContext { RequestServices = services };
            var context = new TokenValidatedContext(http, new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
            { Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, role)], "test")) };
            await AccountTokenValidator.ValidateAsync(context); return context;
        }
        var promoted = await Validate("Tuner");
        Check(promoted.Principal!.IsInRole("ProductionManager") && !promoted.Principal.IsInRole("Tuner"), "stale token adopts current manager permissions");
        user.SetRole("Workshop"); await db.SaveChangesAsync();
        var demoted = await Validate("Administrator");
        Check(!demoted.Principal!.IsInRole("Administrator") && demoted.Principal.IsInRole("Workshop"), "stale administrator role cannot retain deletion permission after demotion");
        // A stale legacy Role must never override explicit assignments in UserRoles.
        user.SetRole("Administrator");
        var tunerRole = new Role("Tuner", "تیونر");
        db.AddRange(tunerRole, new UserRole(user.Id, tunerRole.Id)); await db.SaveChangesAsync();
        var assigned = await Validate("Administrator");
        Check(assigned.Principal!.IsInRole("Tuner") && !assigned.Principal.IsInRole("Administrator"),
            "explicit current role assignments take priority over the legacy role field");
        user.Deactivate(); await db.SaveChangesAsync();
        Check((await Validate("Administrator")).Result?.Failure is not null, "inactive accounts remain blocked");

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var page = new ExportWarehouse();
        var rows = new List<ExportWarehouseItemDto> {
            new() { Id=Guid.NewGuid(), ItemType="bowl", Location=4, ProductionCode="B1" },
            new() { Id=Guid.NewGuid(), ItemType="bowl", Location=4, ProductionCode="B2" },
            new() { Id=Guid.NewGuid(), ItemType="handpan", Location=4, ProductionCode="H1" },
            new() { Id=Guid.NewGuid(), ItemType="bowl", Location=1, ProductionCode="OTHER" } };
        typeof(ExportWarehouse).GetField("_items", flags)!.SetValue(page, rows);
        var selected = (HashSet<Guid>)typeof(ExportWarehouse).GetField("_selected", flags)!.GetValue(page)!;
        void All(bool value) => typeof(ExportWarehouse).GetMethod("ToggleAll", flags)!.Invoke(page, [new Microsoft.AspNetCore.Components.ChangeEventArgs { Value=value }]);
        All(true); Check(selected.SetEquals(rows.Take(2).Select(x=>x.Id)), "select all is scoped to the active warehouse and item tab");
        typeof(ExportWarehouse).GetMethod("Toggle", flags)!.Invoke(page, [rows[0].Id, false]);
        Check(selected.SetEquals([rows[1].Id]), "individual items can be deselected after selecting all");
        typeof(ExportWarehouse).GetMethod("SelectInventoryTab", flags)!.Invoke(page, ["handpans"]); All(true);
        Check(selected.SetEquals([rows[2].Id]), "instrument tab supports select all without hidden bowl selections");
        All(false); Check(selected.Count==0, "select-all checkbox can clear the active selection");
        typeof(ExportWarehouse).GetMethod("SelectInventoryTab", flags)!.Invoke(page, ["bowls"]);
        typeof(ExportWarehouse).GetField("_search", flags)!.SetValue(page, "B2"); All(true);
        Check(selected.SetEquals([rows[1].Id]), "filtered select-all includes only matching visible goods");
    }
}
