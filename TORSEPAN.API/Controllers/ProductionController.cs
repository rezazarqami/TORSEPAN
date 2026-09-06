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

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductionDeletionService _deletionService;
    private readonly IProductionRollbackService _rollbackService;
    private readonly TORSEPANDbContext _db;

    public ProductionController(IMediator mediator, IProductionDeletionService deletionService, IProductionRollbackService rollbackService, TORSEPANDbContext db)
    {
        _mediator = mediator;
        _deletionService = deletionService;
        _rollbackService = rollbackService;
        _db = db;
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
    public async Task<IActionResult> Warehouse()
        => Ok(await _mediator.Send(new GetWarehouseInventoryQuery()));

    [HttpGet("warehouse/{handpanId:guid}/details")]
    public async Task<IActionResult> WarehouseDetails(Guid handpanId)
    {
        var items = await _mediator.Send(new GetWarehouseInventoryQuery(handpanId));
        var item = items.FirstOrDefault();
        return item is null ? NotFound() : Ok(item);
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
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Sell(Guid handpanId, [FromBody] SellHandpanRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var error = await SellOneAsync(handpanId, request);
            if (error is not null) return BadRequest(error);
            await transaction.CommitAsync();
            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("sales/bulk")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> SellBulk([FromBody] BulkSellHandpansRequest request)
    {
        var ids = request.HandpanIds.Distinct().ToList();
        if (ids.Count == 0) return BadRequest("حداقل یک ساز را انتخاب کنید.");
        if (ids.Count > 200) return BadRequest("در هر مرحله حداکثر ۲۰۰ ساز قابل ثبت است.");
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var id in ids)
            {
                var error = await SellOneAsync(id, request.Sale);
                if (error is not null) return BadRequest(error);
            }
            await transaction.CommitAsync();
            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{handpanId:guid}/sale")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> UpdateSale(Guid handpanId, [FromBody] SellHandpanRequest request)
    {
        var party = request.PartyId.HasValue ? await _db.AccountingParties.FirstOrDefaultAsync(x => x.Id == request.PartyId && x.IsActive) : null;
        if (request.PartyId.HasValue && party is null) return BadRequest("شخص انتخاب‌شده معتبر نیست.");
        var handpan = await _db.Handpans.FirstOrDefaultAsync(x => x.Id == handpanId);
        if (handpan is null) return NotFound();
        try { handpan.UpdateSaleDetails(party?.Name ?? request.BuyerName, request.Price, request.Destination); }
        catch (InvalidOperationException) { return BadRequest("این ساز در وضعیت فروخته‌شده نیست."); }
        var document = await _db.AccountingDocuments.FirstOrDefaultAsync(x => x.HandpanId == handpanId && x.Type == AccountingDocumentType.Revenue);
        if (request.Price.HasValue)
        {
            var paid = request.IsPaid ? request.Price.Value : document?.PaidAmount ?? 0;
            if (document is null)
                _db.AccountingDocuments.Add(new AccountingDocument(AccountingDocumentType.Revenue, $"فروش ساز {handpan.SerialNumber}", request.Price.Value, paid, party?.Id, handpanId, request.IsPaid ? null : request.DueDate, SaleNotes(request.Destination), CurrentUserId()));
            else
                try { document.UpdateSale(request.Price.Value, paid, party?.Id, request.IsPaid ? null : request.DueDate, SaleNotes(request.Destination)); }
                catch (ArgumentOutOfRangeException) { return BadRequest("قیمت نمی‌تواند از مبلغی که قبلاً دریافت شده کمتر باشد."); }
        }
        else if (document is not null && document.PaidAmount == 0)
            _db.AccountingDocuments.Remove(document);
        else if (document is not null)
            return BadRequest("به دلیل ثبت دریافت وجه، قیمت این فروش نمی‌تواند خالی شود.");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> SellOneAsync(Guid handpanId, SellHandpanRequest request)
    {
        var party = request.PartyId.HasValue ? await _db.AccountingParties.FirstOrDefaultAsync(x => x.Id == request.PartyId && x.IsActive) : null;
        if (request.PartyId.HasValue && party is null) return "شخص انتخاب‌شده معتبر نیست.";
        var serial = await _db.Handpans.Where(x => x.Id == handpanId).Select(x => x.SerialNumber).FirstOrDefaultAsync();
        if (serial is null) return "یکی از سازهای انتخاب‌شده پیدا نشد.";
        await _mediator.Send(new SellHandpanCommand(handpanId, party?.Name ?? request.BuyerName, request.Price, request.Destination));
        if (request.Price.HasValue)
            _db.AccountingDocuments.Add(new AccountingDocument(AccountingDocumentType.Revenue, $"فروش ساز {serial}", request.Price.Value, request.IsPaid ? request.Price.Value : 0, party?.Id, handpanId, request.IsPaid ? null : request.DueDate, SaleNotes(request.Destination), CurrentUserId()));
        await _db.SaveChangesAsync();
        return null;
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private static string? SaleNotes(string? destination) => string.IsNullOrWhiteSpace(destination) ? null : $"مقصد: {destination.Trim()}";

    [HttpGet("sales")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Sales()
    {
        var items = (await _mediator.Send(new GetSalesQuery())).ToList();
        var ids = items.Where(x => !x.IsBowl).Select(x => x.HandpanId).ToList();
        var documents = await _db.AccountingDocuments.AsNoTracking().Where(x => x.HandpanId.HasValue && ids.Contains(x.HandpanId.Value) && x.Type == AccountingDocumentType.Revenue).ToListAsync();
        foreach (var item in items.Where(x => !x.IsBowl))
        {
            var document = documents.FirstOrDefault(x => x.HandpanId == item.HandpanId);
            if (document is null) continue;
            item.PartyId = document.PartyId; item.PaidAmount = document.PaidAmount; item.DueDate = document.DueDate;
        }
        return Ok(items);
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
public sealed record SellHandpanRequest(string? BuyerName, decimal? Price, string? Destination, Guid? PartyId, bool IsPaid, DateTime? DueDate);
public sealed record BulkSellHandpansRequest(IReadOnlyCollection<Guid> HandpanIds, SellHandpanRequest Sale);
