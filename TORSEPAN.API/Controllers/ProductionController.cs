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

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductionDeletionService _deletionService;

    public ProductionController(IMediator mediator, IProductionDeletionService deletionService)
    {
        _mediator = mediator;
        _deletionService = deletionService;
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
    public async Task<IActionResult> Dashboard()
        => Ok(await _mediator.Send(new GetProductionDashboardQuery()));

    [HttpGet("status-count")]
    public async Task<IActionResult> StatusCount()
        => Ok(await _mediator.Send(new GetProductionCountByStatusQuery()));

    [HttpGet("report")]
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
    public async Task<IActionResult> Sell(Guid handpanId, [FromBody] SellHandpanRequest request)
    { await _mediator.Send(new SellHandpanCommand(handpanId, request.BuyerName, request.Price, request.Destination)); return NoContent(); }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales() => Ok(await _mediator.Send(new GetSalesQuery()));

    [HttpDelete("{handpanId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid handpanId, CancellationToken cancellationToken)
        => await _deletionService.DeleteHandpanAsync(handpanId, cancellationToken) ? NoContent() : NotFound();
}
public sealed record SellHandpanRequest(string BuyerName, decimal Price, string Destination);
