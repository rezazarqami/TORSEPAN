using MediatR;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Handpans.Queries.GetCurrentProductionStage;
using TORSEPAN.Application.Handpans.Queries.GetProductionTimeline;
using TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;
using TORSEPAN.Application.ProductionEvents.Commands.CompleteProduction;
using TORSEPAN.Application.ProductionEvents.Commands.CreateProductionEvent;
using TORSEPAN.Application.ProductionEvents.Commands.MoveToWarehouse;
using TORSEPAN.Application.ProductionEvents.Queries.GetFinishedHandpans;
using TORSEPAN.Application.ProductionEvents.Queries.GetHandpanDetails;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionCountByStatus;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventById;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventsByHandpan;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionHistory;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionQueue;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionStageSummary;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionStatistics;
using TORSEPAN.Application.ProductionEvents.Queries.GetReadyForPackaging;
using TORSEPAN.Application.ProductionEvents.Queries.GetRejectedHandpans;
using TORSEPAN.Application.ProductionEvents.Queries.GetStageWorkload;
using TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Services;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProductionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductionDeletionService _deletionService;
    private readonly IProductionRollbackService _rollbackService;
    private readonly TORSEPANDbContext _db;
    private readonly GuaranteeServiceClient _guaranteeService;
    private readonly ILogger<ProductionController> _logger;

    public ProductionController(IMediator mediator, IProductionDeletionService deletionService, IProductionRollbackService rollbackService, TORSEPANDbContext db, GuaranteeServiceClient guaranteeService, ILogger<ProductionController> logger)
    {
        _mediator = mediator;
        _deletionService = deletionService;
        _rollbackService = rollbackService;
        _db = db;
        _guaranteeService = guaranteeService;
        _logger = logger;
    }

    [HttpPost("event")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateProductionEventCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("stage")]
    public async Task<IActionResult> ChangeStage([FromBody] ChangeProductionStageCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{handpanId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid handpanId)
    {
        await _mediator.Send(new CompleteProductionCommand(handpanId));
        return NoContent();
    }

    [HttpPut("{handpanId:guid}/warehouse")]
    public async Task<IActionResult> MoveToWarehouse(Guid handpanId)
    {
        await _mediator.Send(new MoveToWarehouseCommand(handpanId));
        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics()
        => Ok(await _mediator.Send(new GetProductionStatisticsQuery()));

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? date)
        => Ok(await _mediator.Send(new GetProductionDashboardQuery(date)));

    [HttpGet("status-count")]
    public async Task<IActionResult> StatusCount()
        => Ok(await _mediator.Send(new GetProductionCountByStatusQuery()));

    [HttpGet("report")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Report([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? userId, [FromQuery] ProductionAction? action, [FromQuery] EventResult? result)
        => Ok(await _mediator.Send(new GetProductionReportQuery(from, to, userId, action, result)));

    [HttpGet("stage-summary")]
    public async Task<IActionResult> StageSummary()
        => Ok(await _mediator.Send(new GetProductionStageSummaryQuery()));

    [HttpGet("stage-workload")]
    public async Task<IActionResult> StageWorkload()
        => Ok(await _mediator.Send(new GetStageWorkloadQuery()));

    [HttpGet("warehouse")]
    public async Task<IActionResult> Warehouse(CancellationToken ct)
    {
        var items = (await _mediator.Send(new GetWarehouseInventoryQuery(), ct)).ToList();
        await PopulateWarehouseNotesAsync(items, ct);
        return Ok(items);
    }

    [HttpGet("warehouse/{handpanId:guid}/details")]
    public async Task<IActionResult> WarehouseDetails(Guid handpanId, CancellationToken ct)
    {
        var items = (await _mediator.Send(new GetWarehouseInventoryQuery(handpanId), ct)).ToList();
        var item = items.FirstOrDefault();
        if (item is not null) await PopulateWarehouseNotesAsync(items, ct);
        return item is null ? NotFound() : Ok(item);
    }

    private async Task PopulateWarehouseNotesAsync(
        List<TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory.GetWarehouseInventoryResponse> items,
        CancellationToken ct)
    {
        if (items.Count == 0) return;
        var ids = items.Select(x => x.HandpanId).ToArray();
        var bowls = await _db.Handpans.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Assembly.TopBowlId, x.Assembly.BottomBowlId }).ToListAsync(ct);
        var notes = await TORSEPAN.API.Services.InstrumentNotes.ForBowlsAsync(_db,
            bowls.SelectMany(x => new[] { x.TopBowlId, x.BottomBowlId }), ct);
        var byHandpan = bowls.ToDictionary(x => x.Id);
        foreach (var item in items)
            if (byHandpan.TryGetValue(item.HandpanId, out var bowl))
                item.Notes = TORSEPAN.API.Services.InstrumentNotes.Combine(notes, bowl.TopBowlId, bowl.BottomBowlId);
    }

    [HttpPost("warehouse/gallery")]
    public async Task<IActionResult> WarehouseGallery([FromBody] WarehouseGalleryRequest request, CancellationToken ct)
    {
        var ids = request.HandpanIds.Distinct().Take(500).ToArray();
        if (ids.Length == 0) return Ok(Array.Empty<object>());
        var photos = await _db.HandpanPhotos.AsNoTracking().Where(x => ids.Contains(x.HandpanId))
            .GroupBy(x => x.HandpanId)
            .Select(x => x.OrderBy(y => y.CreatedAt).Select(y => new { y.HandpanId, y.Id, y.Thumbnail }).First())
            .ToListAsync(ct);
        return Ok(photos);
    }

    [HttpGet("finished")]
    public async Task<IActionResult> Finished()
        => Ok(await _mediator.Send(new GetFinishedHandpansQuery()));

    [HttpGet("ready-for-packaging")]
    public async Task<IActionResult> ReadyForPackaging()
        => Ok(await _mediator.Send(new GetReadyForPackagingQuery()));

    [HttpGet("rejected")]
    public async Task<IActionResult> Rejected()
        => Ok(await _mediator.Send(new GetRejectedHandpansQuery()));

    [HttpGet("queue/{stage}")]
    public async Task<IActionResult> Queue(string stage)
        => Ok(await _mediator.Send(new GetProductionQueueQuery(stage)));

    [HttpGet("{handpanId:guid}")]
    public async Task<IActionResult> Details(Guid handpanId)
        => Ok(await _mediator.Send(new GetHandpanDetailsQuery(handpanId)));

    [HttpGet("{handpanId:guid}/history")]
    public async Task<IActionResult> History(Guid handpanId)
        => Ok(await _mediator.Send(new GetProductionHistoryQuery(handpanId)));

    [HttpGet("event/{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id)
        => Ok(await _mediator.Send(new GetProductionEventByIdQuery(id)));

    [HttpGet("handpan/{handpanId:guid}/events")]
    public async Task<IActionResult> GetHandpanEvents(Guid handpanId)
        => Ok(await _mediator.Send(new GetProductionEventsByHandpanQuery(handpanId)));

    [HttpGet("{serialNumber}/timeline")]
    public async Task<IActionResult> GetTimeline(string serialNumber)
        => Ok(await _mediator.Send(new GetProductionTimelineQuery(serialNumber)));

    [HttpGet("{serialNumber}/current-stage")]
    public async Task<IActionResult> GetCurrentStage(string serialNumber)
        => Ok(await _mediator.Send(new GetCurrentProductionStageQuery(serialNumber)));

    [HttpPost("{handpanId:guid}/sell")]
    [Authorize(Roles = "Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> Sell(Guid handpanId, [FromBody] SellHandpanRequest request)
    {
        var validationError = ValidateWarrantyRequest(request);
        if (validationError is not null) return BadRequest(validationError);
        var serial = await _db.Handpans.Where(x => x.Id == handpanId).Select(x => x.SerialNumber).FirstOrDefaultAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var error = await SellOneAsync(handpanId, request);
            if (error is not null) return BadRequest(error);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        var warranty = await ActivateWarrantyAsync(serial is null ? [] : [serial], request);
        return Ok(new SellHandpansResponse(true, warranty.IsActive, warranty.Error));
    }

    [HttpPost("sales/bulk")]
    [Authorize(Roles = "Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> SellBulk([FromBody] BulkSellHandpansRequest request)
    {
        var ids = request.HandpanIds.Distinct().ToList();
        if (ids.Count == 0) return BadRequest("حداقل یک ساز را انتخاب کنید.");
        if (ids.Count > 200) return BadRequest("در هر مرحله حداکثر ۲۰۰ ساز قابل ثبت است.");

        var validationError = ValidateWarrantyRequest(request.Sale);
        if (validationError is not null) return BadRequest(validationError);
        var serials = await _db.Handpans.Where(x => ids.Contains(x.Id)).Select(x => x.SerialNumber).ToListAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            for (var index = 0; index < ids.Count; index++)
            {
                var count = ids.Count;
                var price = SplitMoney(request.Sale.Price,count,index,request.Sale.PriceCurrency ?? MoneyCurrency.Toman);
                var received = SplitMoney(request.Sale.ReceivedAmount,count,index,request.Sale.PriceCurrency ?? MoneyCurrency.Toman);
                var profit = SplitMoney(request.Sale.NetProfit,count,index,request.Sale.ProfitCurrency ?? MoneyCurrency.Toman);
                var error = await SellOneAsync(ids[index], request.Sale with { Price = price, ReceivedAmount = received, NetProfit = profit });
                if (error is not null) return BadRequest(error);
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        var warranty = await ActivateWarrantyAsync(serials, request.Sale);
        return Ok(new SellHandpansResponse(true, warranty.IsActive, warranty.Error));
    }

    [HttpPut("{handpanId:guid}/sale")]
    [Authorize(Roles = "Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> UpdateSale(Guid handpanId, [FromBody] SellHandpanRequest request)
    {
        var validationError = ValidateWarrantyRequest(request);
        if (validationError is not null) return BadRequest(validationError);
        var party = request.PartyId.HasValue ? await _db.AccountingParties.FirstOrDefaultAsync(x => x.Id == request.PartyId && x.IsActive) : null;
        if (request.PartyId.HasValue && party is null) return BadRequest("شخص انتخاب‌شده معتبر نیست.");
        var handpan = await _db.Handpans.FirstOrDefaultAsync(x => x.Id == handpanId);
        if (handpan is null) return NotFound();
        var attributionError = await ValidateAttributionAsync(request);
        if (attributionError is not null) return BadRequest(attributionError);
        var destination = request.IsExportSale == true ? request.Destination : null;
        var financials = request.FinancialsSpecified || request.PriceCurrency.HasValue || request.NetProfit.HasValue || request.ProfitCurrency.HasValue;
        var priceCurrency = request.PriceCurrency ?? handpan.SaleCurrency;
        var oldCurrency = handpan.SaleCurrency;
        try { handpan.UpdateSaleDetails(party?.Name ?? request.BuyerName, party?.Phone ?? request.BuyerPhoneNumber, request.Price, request.Destination, request.IsExportSale); }
        catch (InvalidOperationException) { return BadRequest("این ساز در وضعیت فروخته‌شده نیست."); }
        var document = await _db.AccountingDocuments.FirstOrDefaultAsync(x => x.HandpanId == handpanId && x.Type == AccountingDocumentType.Revenue);
        if(document is not null && document.PaidAmount>0 && priceCurrency!=oldCurrency)return BadRequest("ارز فروش دارای دریافت وجه را نمی‌توان تغییر داد؛ ابتدا دریافت ثبت‌شده را اصلاح کنید.");
        if (request.Price.HasValue)
        {
            var paid = Math.Min(request.Price.Value, request.ReceivedAmount ?? document?.PaidAmount ?? 0);
            if (document is null)
                _db.AccountingDocuments.Add(new AccountingDocument(AccountingDocumentType.Revenue, $"فروش ساز {handpan.SerialNumber}", request.Price.Value, paid, party?.Id, handpanId, null, SaleNotes(destination), CurrentUserId()));
            else
                try { document.UpdateSale(request.Price.Value, paid, party?.Id, null, SaleNotes(destination)); }
                catch (ArgumentOutOfRangeException) { return BadRequest("قیمت نمی‌تواند از مبلغی که قبلاً دریافت شده کمتر باشد."); }
        }
        else if (document is not null && document.PaidAmount == 0)
            _db.AccountingDocuments.Remove(document);
        else if (document is not null)
            return BadRequest("به دلیل ثبت دریافت وجه، قیمت این فروش نمی‌تواند خالی شود.");
        if(financials){await new SaleProfitService(_db).UpdateAsync(handpan,request.NetProfit,request.ProfitCurrency ?? handpan.ProfitCurrency,true);handpan.SetSaleFinancials(priceCurrency,request.NetProfit,request.ProfitCurrency ?? handpan.ProfitCurrency);}
        foreach(var saleDocument in _db.ChangeTracker.Entries<AccountingDocument>().Where(x=>x.Entity.HandpanId==handpanId&&x.State!=EntityState.Deleted))saleDocument.Entity.SetCurrency(handpan.SaleCurrency);
        handpan.SetSaleAttribution(request.LeadSourceId, request.ReferrerId);
        handpan.ActivateWarranty();
        await _db.SaveChangesAsync();
        if (_db.Database.CurrentTransaction is not null) return Ok(new SellHandpansResponse(true,false,"همگام‌سازی گارانتی در صف است."));
        var warranty = await ActivateWarrantyAsync([handpan.SerialNumber], request);
        return Ok(new SellHandpansResponse(true, warranty.IsActive, warranty.Error));
    }

    private async Task<string?> SellOneAsync(Guid handpanId, SellHandpanRequest request)
    {
        var party = request.PartyId.HasValue ? await _db.AccountingParties.FirstOrDefaultAsync(x => x.Id == request.PartyId && x.IsActive) : null;
        if (request.PartyId.HasValue && party is null) return "شخص انتخاب‌شده معتبر نیست.";
        var handpan = await _db.Handpans.FirstOrDefaultAsync(x => x.Id == handpanId);
        if (handpan is null) return "یکی از سازهای انتخاب‌شده پیدا نشد.";
        var serial = handpan.SerialNumber;
        if(handpan.Stage is not (ProductionStage.FinishedWarehouse or ProductionStage.ExportWarehouse))return "این ساز قبلاً فروخته شده یا دیگر در انبار نیست؛ فهرست را تازه کنید.";

        var attributionError = await ValidateAttributionAsync(request);
        if (attributionError is not null) return attributionError;
        await _mediator.Send(new SellHandpanCommand(handpanId, party?.Name ?? request.BuyerName, party?.Phone ?? request.BuyerPhoneNumber, request.Price, request.Destination, (request.IsExportSale ?? false)));
        handpan.SetSaleFinancials(request.PriceCurrency ?? MoneyCurrency.Toman, request.NetProfit, request.ProfitCurrency ?? MoneyCurrency.Toman);
        await new SaleProfitService(_db).UpdateAsync(handpan,request.NetProfit,request.ProfitCurrency ?? MoneyCurrency.Toman);
        handpan.SetSaleAttribution(request.LeadSourceId, request.ReferrerId);
        handpan.ActivateWarranty();
        if (request.Price.HasValue)
            _db.AccountingDocuments.Add(new AccountingDocument(AccountingDocumentType.Revenue, $"فروش ساز {serial}", request.Price.Value, Math.Min(request.Price.Value, request.ReceivedAmount ?? 0), party?.Id, handpanId, null, SaleNotes(request.Destination), CurrentUserId()));
        foreach(var saleDocument in _db.ChangeTracker.Entries<AccountingDocument>().Where(x=>x.State==EntityState.Added&&x.Entity.HandpanId==handpanId))saleDocument.Entity.SetCurrency(handpan.SaleCurrency);
        await _db.SaveChangesAsync();
        return null;
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private async Task<string?> ValidateAttributionAsync(SellHandpanRequest request)
    {
        if (!request.LeadSourceId.HasValue)
            return request.ReferrerId.HasValue ? "برای معرف، نحوه آشنایی را نیز انتخاب کنید." : null;
        var source = await _db.SaleLeadSources.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.LeadSourceId && x.IsActive);
        if (source is null) return "نحوه آشنایی انتخاب‌شده معتبر نیست.";

        if (!source.RequiresReferrer && request.ReferrerId.HasValue) return "معرف فقط برای روش آشنایی «معرف» قابل انتخاب است.";
        if (request.ReferrerId.HasValue && !await _db.SalesReferrers.AnyAsync(x => x.Id == request.ReferrerId && x.IsActive))
            return "معرف انتخاب‌شده معتبر نیست.";
        return null;
    }
    private static string? SaleNotes(string? destination) => string.IsNullOrWhiteSpace(destination) ? null : $"مقصد: {destination.Trim()}";

    [HttpGet("sales")]
    [Authorize(Roles = "Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> Sales()
    {
        var items = (await _mediator.Send(new GetSalesQuery())).ToList();
        var ids = items.Where(x => !x.IsBowl).Select(x => x.HandpanId).ToList();
        var locallyActiveWarrantyIds = (await _db.Handpans.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.WarrantyActivatedAt.HasValue)
            .Select(x => x.Id).ToListAsync()).ToHashSet();
        var documents = await _db.AccountingDocuments.AsNoTracking().Where(x => x.HandpanId.HasValue && ids.Contains(x.HandpanId.Value) && x.Type == AccountingDocumentType.Revenue).ToListAsync();
        foreach (var item in items.Where(x => !x.IsBowl))
        {
            var document = documents.FirstOrDefault(x => x.HandpanId == item.HandpanId);
            if (document is null) continue;
            item.PartyId = document.PartyId; item.PaidAmount = document.PaidAmount; item.DueDate = document.DueDate;
        }
        var partyIds = documents.Where(x => x.PartyId.HasValue).Select(x => x.PartyId!.Value).Distinct().ToList();
        var partyPhones = await _db.AccountingParties.AsNoTracking()
            .Where(x => partyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Phone ?? string.Empty);
        var salePhones = await _db.Handpans.AsNoTracking().Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.BuyerPhoneNumber ?? string.Empty);
        var attribution = await _db.Handpans.AsNoTracking().Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.SaleLeadSourceId, x.SalesReferrerId });
        foreach (var item in items.Where(x => !x.IsBowl))
        {
            item.BuyerPhoneNumber = item.PartyId.HasValue
                ? partyPhones.GetValueOrDefault(item.PartyId.Value, salePhones.GetValueOrDefault(item.HandpanId, string.Empty))
                : salePhones.GetValueOrDefault(item.HandpanId, string.Empty);
            item.IsWarrantyActive = locallyActiveWarrantyIds.Contains(item.HandpanId);
            if (attribution.TryGetValue(item.HandpanId, out var source))
            { item.LeadSourceId = source.SaleLeadSourceId; item.ReferrerId = source.SalesReferrerId; }
        }
        try
        {
            var statuses = await _guaranteeService.GetStatusesAsync(items.Where(x => !x.IsBowl).Select(x => x.SerialNumber));
            foreach (var item in items.Where(x => !x.IsBowl))
            {
                var warranty = statuses.GetValueOrDefault(item.SerialNumber);
                item.IsWarrantyActive = locallyActiveWarrantyIds.Contains(item.HandpanId) || warranty?.IsActive == true;
                item.WarrantyOwnerName = warranty?.OwnerFullName ?? string.Empty;
                item.WarrantyOwnerPhoneNumber = warranty?.OwnerPhoneNumber ?? string.Empty;
                item.WarrantyOwnerCity = warranty?.OwnerCity ?? string.Empty;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Warranty statuses could not be loaded for the sales list.");
        }
        return Ok(items);
    }

    private static string? ValidateWarrantyRequest(SellHandpanRequest request)
    {
        // Warranty activation during a sale is intentionally checkbox-only.
        // Identity fields are required only in the public guarantee application.
        if (!SaleMoney.Valid(request.Price,request.PriceCurrency ?? MoneyCurrency.Toman)||!SaleMoney.Valid(request.ReceivedAmount,request.PriceCurrency ?? MoneyCurrency.Toman)||!SaleMoney.Valid(request.NetProfit,request.ProfitCurrency ?? MoneyCurrency.Toman))return "مبلغ و ارز را بررسی کنید؛ تومان بدون اعشار و دلار حداکثر دو رقم اعشار است.";
        if(request.ReceivedAmount.HasValue&&(!request.Price.HasValue||request.ReceivedAmount>request.Price))return "مبلغ دریافتی نباید از قیمت فروش بیشتر باشد.";
        return null;
    }
    private static decimal? SplitMoney(decimal? amount,int count,int index,MoneyCurrency currency){if(!amount.HasValue)return null;var factor=currency==MoneyCurrency.Usd?100m:1m;var each=Math.Floor(amount.Value*factor/count)/factor;return index==count-1?amount.Value-each*(count-1):each;}

    private async Task<WarrantyActivationResult> ActivateWarrantyAsync(IEnumerable<string> serials, SellHandpanRequest request)
    {
        var party = request.PartyId.HasValue ? await _db.AccountingParties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PartyId) : null;
        var name = party?.Name ?? request.BuyerName ?? "ثبت از فروش";
        var phone = party?.Phone ?? request.BuyerPhoneNumber ?? string.Empty;
        foreach (var serial in serials)
        {
            var result = await _guaranteeService.ActivateAsync(serial, name, phone, string.Empty);
            if (!result.IsActive) return result;
        }
        return new WarrantyActivationResult(true, null);
    }

    [HttpDelete("{handpanId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid handpanId, CancellationToken cancellationToken)
        => await _deletionService.DeleteHandpanAsync(handpanId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{handpanId:guid}/rollback")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Rollback(Guid handpanId, CancellationToken cancellationToken)
        => await _rollbackService.RollbackHandpanAsync(handpanId, cancellationToken) ? NoContent() : BadRequest();
}
public sealed record SellHandpanRequest(string? BuyerName, string? BuyerPhoneNumber, decimal? Price, decimal? ReceivedAmount, string? Destination, Guid? PartyId, bool IsPaid, DateTime? DueDate, bool ActivateWarranty, bool? IsExportSale, Guid? LeadSourceId, Guid? ReferrerId, MoneyCurrency? PriceCurrency=null, decimal? NetProfit=null, MoneyCurrency? ProfitCurrency=null, bool FinancialsSpecified=false);
public sealed record BulkSellHandpansRequest(IReadOnlyCollection<Guid> HandpanIds, SellHandpanRequest Sale);
public sealed record SellHandpansResponse(bool SaleRegistered, bool WarrantyActivated, string? Warning);
public sealed record WarehouseGalleryRequest(IReadOnlyCollection<Guid> HandpanIds);
