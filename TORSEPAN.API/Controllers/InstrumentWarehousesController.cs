using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/instrument-warehouses"), Authorize]
public sealed class InstrumentWarehousesController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.InstrumentWarehouses.AsNoTracking()
        .OrderBy(x => x.Id).Select(x => new { x.Id, x.Name, x.Version }).ToListAsync(ct));

    [HttpPut("{id:int}"), Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Rename(int id, WarehouseNameRequest request, CancellationToken ct)
    {
        await using var tx = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var item = await db.InstrumentWarehouses.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (request.ExpectedVersion == Guid.Empty || item.Version != request.ExpectedVersion)
            return Conflict(new { Message = "نام انبار تغییر کرده؛ اطلاعات تازه را دریافت کنید." });
        try { item.Rename(request.Name); }
        catch (ArgumentException error) { return BadRequest(new { Message = error.Message }); }
        if (await db.InstrumentWarehouses.AnyAsync(x => x.Id != id && x.Name.ToUpper() == item.Name.ToUpper(), ct))
            return Conflict(new { Message = "این نام برای انبار دیگری استفاده شده است." });
        try
        {
            await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct);
            return Ok(new { item.Id, item.Name, item.Version });
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new { Message = "نام انبار هم‌زمان تغییر کرد؛ دوباره دریافت کنید." }); }
    }

    [HttpPost("transfer"), Authorize(Roles = "Administrator,ProductionManager,QualityControl")]
    public async Task<IActionResult> Transfer(WarehouseTransferRequest request, CancellationToken ct)
    {
        if ((request.ExportLocation.HasValue ? !Enum.IsDefined(request.ExportLocation.Value) : !InstrumentWarehouse.IsValidId(request.WarehouseId)) || request.Items is null || request.Items.Count is < 1 or > 200
            || request.Items.Any(x => x.HandpanId == Guid.Empty || x.ExpectedVersion == Guid.Empty)
            || request.Items.Select(x => x.HandpanId).Distinct().Count() != request.Items.Count)
            return BadRequest(new { Message = "سازها و انبار مقصد را بررسی کنید." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var target = await db.InstrumentWarehouses.SingleOrDefaultAsync(x => x.Id == request.WarehouseId, ct);
        if (!request.ExportLocation.HasValue && target is null) return NotFound();
        var ids = request.Items.Select(x => x.HandpanId).ToArray();
        var items = await db.Handpans.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (items.Count != ids.Length) return NotFound(new { Message = "یک یا چند ساز پیدا نشد." });
        if (items.Any(x => x.SoldAt.HasValue || x.Stage is not (ProductionStage.FinishedWarehouse or ProductionStage.ExportWarehouse)))
            return Conflict(new { Message = "فقط سازهای موجود در انبار قابل انتقال‌اند؛ وضعیت سازها را تازه کنید." });
        var expected = request.Items.ToDictionary(x => x.HandpanId, x => x.ExpectedVersion);
        if (items.Any(x => x.OfflineVersion != expected[x.Id]))
            return Conflict(new { Message = "یک ساز هم‌زمان تغییر کرده؛ اطلاعات تازه را دریافت کنید." });
        var names = await db.InstrumentWarehouses.ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        var targetName = request.ExportLocation is { } destination ? $"انبار صادراتی — {destination}" : target!.Name;
        foreach (var handpan in items)
        {
            var source = handpan.Stage == ProductionStage.ExportWarehouse ? $"انبار صادراتی — {handpan.ExportWarehouseLocation}" : names[handpan.QualityWarehouseId];
            if (request.ExportLocation is { } location)
            {
                if (handpan.Stage == ProductionStage.ExportWarehouse && handpan.ExportWarehouseLocation == location) continue;
                handpan.MoveToExportWarehouse(location);
            }
            else
            {
                if (handpan.Stage == ProductionStage.FinishedWarehouse && handpan.QualityWarehouseId == target!.Id) continue;
                handpan.SetQualityWarehouse(target!.Id);
                if (handpan.Stage == ProductionStage.ExportWarehouse)
                {
                    handpan.ChangeStage(ProductionStage.FinishedWarehouse);
                    handpan.SetPackagingDestination(null);
                }
            }
            db.ProductionEvents.Add(new ProductionEvent(handpan.Id, handpan.AssemblyId, null, actor,
                ProductionAction.WarehouseTransfer, EventResult.Completed, null, $"انتقال انبار از {source} به {targetName}"));
        }
        try
        {
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Ok(items.Select(x => new { HandpanId = x.Id, x.QualityWarehouseId, Stage = x.Stage.ToString(), ExportLocation = x.ExportWarehouseLocation, Version = x.OfflineVersion }));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new { Message = "انتقال به دلیل تغییر هم‌زمان انجام نشد؛ فهرست را تازه کنید." }); }
    }
}
public sealed record WarehouseNameRequest(string Name, Guid ExpectedVersion);
public sealed record WarehouseTransferItem(Guid HandpanId, Guid ExpectedVersion);
public sealed record WarehouseTransferRequest(int WarehouseId, IReadOnlyList<WarehouseTransferItem> Items, ExportWarehouseLocation? ExportLocation = null);
