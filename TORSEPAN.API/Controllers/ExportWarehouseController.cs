using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Application.Sales;
using System.Security.Claims;
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

    [HttpPost("ship")]
    [Authorize(Roles = "Workshop,Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> Ship([FromBody] ExportItemsShipmentRequest request, CancellationToken ct)
    {
        var items = request.Items?.Distinct().ToArray() ?? [];
        if (items.Length == 0 || items.Any(x => x is null || x.Id == Guid.Empty || x.ItemType is not ("bowl" or "handpan")))
            return BadRequest("کالاهای انتخاب‌شده معتبر نیستند.");
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        var method = string.IsNullOrWhiteSpace(request.ShippingMethod) ? null : request.ShippingMethod.Trim().ToLowerInvariant();
        if (method is not null && method is not ("air" or "land" or "sea")) return BadRequest("روش ارسال معتبر نیست.");
        var bowlIds = items.Where(x => x.ItemType == "bowl").Select(x => x.Id).ToArray();
        var handpanIds = items.Where(x => x.ItemType == "handpan").Select(x => x.Id).ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var bowls = await db.Bowls.Where(x => bowlIds.Contains(x.Id)).ToListAsync(ct);
        var handpans = await db.Handpans.Where(x => handpanIds.Contains(x.Id)).ToListAsync(ct);
        if (bowls.Count != bowlIds.Length || handpans.Count != handpanIds.Length ||
            bowls.Any(x => x.Stage != ProductionStage.ExportWarehouse) || handpans.Any(x => x.Stage != ProductionStage.ExportWarehouse))
            return Conflict("همه کالاهای انتخاب‌شده باید در انبار صادراتی موجود باشند. فهرست را تازه کنید.");
        var description = ExportSaleMetadata.Encode(request.BuyerName, request.PartyId, request.Destination, method, request.IsSettled);
        foreach (var bowl in bowls)
        {
            bowl.ChangeStage(ProductionStage.Sold);
            bowl.CompleteProduction();
            db.ProductionEvents.Add(new ProductionEvent(null, null, bowl.Id, actor, ProductionAction.Sale, EventResult.Completed, null, description));
        }
        foreach (var handpan in handpans)
        {
            handpan.Sell(request.BuyerName, null, null, request.Destination, true, actor);
            db.ProductionEvents.Add(new ProductionEvent(handpan.Id, handpan.AssemblyId, null, actor, ProductionAction.Sale, EventResult.Completed, null, description));
        }
        // Serialize concurrent shipments and commit the entire selection together.
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is Npgsql.PostgresException { SqlState: "40001" } || ex.InnerException is Npgsql.PostgresException { SqlState: "40001" })
        { return Conflict("موجودی هم‌زمان تغییر کرده؛ فهرست را تازه کنید و دوباره تلاش کنید."); }
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

public sealed record ExportShipmentItem(Guid Id, string ItemType);
public sealed record ExportItemsShipmentRequest(IReadOnlyCollection<ExportShipmentItem>? Items, string? BuyerName,
    Guid? PartyId, string? Destination, string? ShippingMethod, bool? IsSettled);
