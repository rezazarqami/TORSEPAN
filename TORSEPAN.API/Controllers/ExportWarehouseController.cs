using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Application.Sales;
using System.Security.Claims;
using System.Text.Json;
using TORSEPAN.API.Services;
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
                (int)(x.ExportWarehouseLocation ?? ExportWarehouseLocation.Turkey)) {
                Inventory = new TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory.GetWarehouseInventoryResponse {
                    HasDesign=db.ProductionEvents.Any(e=>e.BowlId==x.Assembly.TopBowlId&&e.Action==ProductionAction.Design&&e.Result==EventResult.Completed&&!e.Description.EndsWith(":ساده")),
                    TopNoteCount=x.Assembly.TopBowl.NoteCount,BottomNoteCount=x.Assembly.BottomBowl.HasNotes?x.Assembly.BottomBowl.NoteCount:0,
                    HandpanId=x.Id, SerialNumber=x.SerialNumber, TopBowlCode=x.Assembly.TopBowl.ProductionCode,
                    BottomBowlCode=x.Assembly.BottomBowl.ProductionCode,MaterialName=x.Assembly.TopBowl.Material.Name,
                    ScaleName=x.Scale!=null?x.Scale.Name:"—",NoteCount=x.NoteCount,CreatedAt=x.CreatedAt,WarehouseEntryDate=x.UpdatedAt
                }})
            .ToListAsync(ct);
        foreach(var row in handpans){if(row.Inventory is not { } item)continue;item.BottomBowlCode=LegacyStockRecord.DisplayCode(item.BottomBowlCode);var parsed=ScaleFamilyNames.Parse(item.ScaleName);item.NoteCount??=parsed.NoteCount;if(parsed.FamilyName is not null)item.ScaleName=item.NoteCount.HasValue?$"{parsed.FamilyName} {item.NoteCount}":$"{parsed.FamilyName} — تعداد نت نامشخص";if(item.TopNoteCount.HasValue&&item.BottomNoteCount.HasValue)item.ScaleName+=$" ({item.TopNoteCount}+{item.BottomNoteCount})";}
        return Ok(bowls.Concat(handpans).OrderBy(x => x.Location).ThenBy(x => x.ProductionCode));
    }

    [HttpPost("ship")]
    [Authorize(Roles = "Workshop,Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> Ship([FromBody] ExportItemsShipmentRequest request, CancellationToken ct)
    {
        var items = request.Items?.Distinct().ToArray() ?? [];
        if (items.Length is 0 or >200 || items.Any(x => x is null || x.Id == Guid.Empty || x.ItemType is not ("bowl" or "handpan")))
            return BadRequest("کالاهای انتخاب‌شده معتبر نیستند.");
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        var method = string.IsNullOrWhiteSpace(request.ShippingMethod) ? null : request.ShippingMethod.Trim().ToLowerInvariant();
        if (method is not null && method is not ("air" or "land" or "sea")) return BadRequest("روش ارسال معتبر نیست.");
        var currency=request.PriceCurrency??MoneyCurrency.Toman;var profitCurrency=request.ProfitCurrency??MoneyCurrency.Toman;
        if(!SaleMoney.Valid(request.Price,currency)||!SaleMoney.Valid(request.ReceivedAmount,currency)||!SaleMoney.Valid(request.NetProfit,profitCurrency)||request.ReceivedAmount.HasValue&&(!request.Price.HasValue||request.ReceivedAmount>request.Price))
            return BadRequest(new{Message="مبلغ، ارز و مبلغ دریافتی را بررسی کنید."});
        var party=request.PartyId.HasValue?await db.AccountingParties.SingleOrDefaultAsync(x=>x.Id==request.PartyId&&x.IsActive,ct):null;
        if(request.PartyId.HasValue&&party is null)return BadRequest(new{Message="خریدار انتخاب‌شده معتبر نیست."});
        if(request.LeadSourceId.HasValue){var source=await db.SaleLeadSources.SingleOrDefaultAsync(x=>x.Id==request.LeadSourceId&&x.IsActive,ct);if(source is null||(!source.RequiresReferrer&&request.ReferrerId.HasValue))return BadRequest(new{Message="نحوه آشنایی یا معرف معتبر نیست."});}
        if(request.ReferrerId.HasValue&&(!request.LeadSourceId.HasValue||!await db.SalesReferrers.AnyAsync(x=>x.Id==request.ReferrerId&&x.IsActive,ct)))return BadRequest(new{Message="معرف معتبر نیست."});
        var buyer=party?.Name??request.BuyerName;var phone=party?.Phone??request.BuyerPhoneNumber;
        var bowlIds = items.Where(x => x.ItemType == "bowl").Select(x => x.Id).ToArray();
        var handpanIds = items.Where(x => x.ItemType == "handpan").Select(x => x.Id).ToArray();
        await using var transaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        var bowls = await db.Bowls.Where(x => bowlIds.Contains(x.Id)).ToListAsync(ct);
        var handpans = await db.Handpans.Where(x => handpanIds.Contains(x.Id)).ToListAsync(ct);
        if (bowls.Count != bowlIds.Length || handpans.Count != handpanIds.Length ||
            bowls.Any(x => x.Stage != ProductionStage.ExportWarehouse) || handpans.Any(x => x.Stage != ProductionStage.ExportWarehouse))
            return Conflict("همه کالاهای انتخاب‌شده باید در انبار صادراتی موجود باشند. فهرست را تازه کنید.");
        var ordered=items.OrderBy(x=>x.ItemType).ThenBy(x=>x.Id).ToArray();
        var profits=new SaleProfitService(db);
        for(var index=0;index<ordered.Length;index++)
        {
            var row=ordered[index];
            var price=Split(request.Price,ordered.Length,index,currency);var received=Split(request.ReceivedAmount,ordered.Length,index,currency);var profit=Split(request.NetProfit,ordered.Length,index,profitCurrency);
            var description=ExportSaleMetadata.Prefix+JsonSerializer.Serialize(new ExportSaleDetails(buyer,request.PartyId,request.Destination,method,request.IsSettled,price,received,profit,currency,profitCurrency,phone,request.IsExportSale,request.LeadSourceId,request.ReferrerId));
            if(row.ItemType=="bowl")
            {
                var bowl=bowls.Single(x=>x.Id==row.Id);bowl.ChangeStage(ProductionStage.Sold);bowl.CompleteProduction();
                db.ProductionEvents.Add(new ProductionEvent(null,null,bowl.Id,actor,ProductionAction.Sale,EventResult.Completed,null,description));
                await profits.AllocateBowlAsync(bowl,profit,profitCurrency);
            }
            else
            {
                var handpan=handpans.Single(x=>x.Id==row.Id);handpan.Sell(buyer,phone,price,request.Destination,request.IsExportSale,actor);
                handpan.SetSaleFinancials(currency,profit,profitCurrency);handpan.SetSaleAttribution(request.LeadSourceId,request.ReferrerId);
                handpan.ActivateWarranty();await profits.UpdateAsync(handpan,profit,profitCurrency);
                db.ProductionEvents.Add(new ProductionEvent(handpan.Id,handpan.AssemblyId,null,actor,ProductionAction.Sale,EventResult.Completed,null,description));
            }
            if(price.HasValue){var document=new AccountingDocument(AccountingDocumentType.Revenue,$"فروش {(row.ItemType=="bowl"?"کاسه":"ساز")} {(row.ItemType=="bowl"?bowls.Single(x=>x.Id==row.Id).ProductionCode:handpans.Single(x=>x.Id==row.Id).SerialNumber)}",price.Value,received??0,request.PartyId,row.ItemType=="handpan"?row.Id:null,null,description,actor);document.SetCurrency(currency);db.AccountingDocuments.Add(document);}
        }
        // Serialize concurrent shipments and commit the entire selection together.
        try
        {
            await db.SaveChangesAsync(ct);
            if(transaction is not null)await transaction.CommitAsync(ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is Npgsql.PostgresException { SqlState: "40001" } || ex.InnerException is Npgsql.PostgresException { SqlState: "40001" })
        { return Conflict("موجودی هم‌زمان تغییر کرده؛ فهرست را تازه کنید و دوباره تلاش کنید."); }
    }

    private static decimal? Split(decimal? value,int count,int index,MoneyCurrency currency){if(!value.HasValue)return null;var factor=currency==MoneyCurrency.Usd?100m:1m;var each=decimal.Floor(value.Value*factor/count)/factor;return index==count-1?value.Value-each*(count-1):each;}

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

public sealed record ExportWarehouseRow(Guid Id, string ItemType, string ProductionCode, string ItemKind, string Detail, int Location)
{
 public TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory.GetWarehouseInventoryResponse? Inventory {get;init;}
}
public sealed record ChangeLocationRequest(ExportWarehouseLocation Location);

public sealed record ExportShipmentItem(Guid Id, string ItemType);
public sealed record ExportItemsShipmentRequest(IReadOnlyCollection<ExportShipmentItem>? Items, string? BuyerName,
    Guid? PartyId, string? Destination, string? ShippingMethod, bool? IsSettled,string? BuyerPhoneNumber=null,decimal? Price=null,decimal? ReceivedAmount=null,decimal? NetProfit=null,MoneyCurrency? PriceCurrency=null,MoneyCurrency? ProfitCurrency=null,bool IsExportSale=true,Guid? LeadSourceId=null,Guid? ReferrerId=null,bool FinancialsSpecified=false);
