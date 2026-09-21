using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/export-warehouse"), Authorize]
public sealed class ExportWarehouseController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var bowls = await db.Bowls.AsNoTracking()
            .Where(x => x.Stage == ProductionStage.ExportWarehouse)
            .Select(x => new ExportWarehouseRow(x.Id, "bowl", x.ProductionCode,
                x.BowlType == BowlType.Top ? "کاسه رو" : "کاسه زیر", x.Material.Name,
                (int)(x.ExportWarehouseLocation ?? ExportWarehouseLocation.Turkey)))
            .ToListAsync(ct);
        var handpans = await db.Handpans.AsNoTracking()
            .Where(x => x.Stage == ProductionStage.ExportWarehouse)
            .Select(x => new ExportWarehouseRow(x.Id, "handpan", x.SerialNumber, "ساز کامل",
                x.Scale != null ? x.Scale.Name : "بدون Scale",
                (int)(x.ExportWarehouseLocation ?? ExportWarehouseLocation.Turkey)))
            .ToListAsync(ct);
        return Ok(bowls.Concat(handpans).OrderBy(x => x.Location).ThenBy(x => x.ProductionCode));
    }

    [HttpPut("{itemType}/{id:guid}/location")]
    [Authorize(Roles = "Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> ChangeLocation(string itemType, Guid id, [FromBody] ChangeLocationRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Location)) return BadRequest("انبار انتخاب‌شده معتبر نیست.");
        if (itemType.Equals("bowl", StringComparison.OrdinalIgnoreCase))
        {
            var item = await db.Bowls.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return NotFound();
            item.ChangeExportWarehouseLocation(request.Location);
        }
        else if (itemType.Equals("handpan", StringComparison.OrdinalIgnoreCase))
        {
            var item = await db.Handpans.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return NotFound();
            item.ChangeExportWarehouseLocation(request.Location);
        }
        else return BadRequest("نوع موجودی معتبر نیست.");
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record ExportWarehouseRow(Guid Id, string ItemType, string ProductionCode, string ItemKind, string Detail, int Location);
public sealed record ChangeLocationRequest(ExportWarehouseLocation Location);
