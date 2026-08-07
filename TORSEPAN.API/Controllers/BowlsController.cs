using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Common.Extensions;
using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Pagination;
using TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;
using TORSEPAN.API.Contracts.Bowls;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BowlsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BowlsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BowlDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAllBowlsQuery(new PageRequest(page, pageSize)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBowlByIdQuery(id),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBowlCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new
        {
            id = result.Value
        });
    }

    [HttpGet("dimpling/{productionCode}")]
    [Authorize(Roles = "Dimpler,Shaper,Workshop,Tuner,FineTuner,QualityControl,Administrator")]
    public async Task<ActionResult> GetForDimpling(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBowlForDimpleQuery(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("dimpling/{productionCode}/complete")]
    [Authorize(Roles = "Dimpler,Shaper,Administrator")]
    public async Task<ActionResult> CompleteDimpling(
        string productionCode,
        [FromBody] CompleteBowlDimpleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlDimpleCommand(productionCode, request.Duration),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/shape/complete")]
    [Authorize(Roles = "Shaper,Administrator")]
    public async Task<ActionResult> CompleteShape(
        string productionCode,
        [FromBody] CompleteBowlDimpleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlShapeCommand(productionCode, request.Duration),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/bake/complete")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> CompleteBake(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlBakeCommand(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/tune/complete")]
    [Authorize(Roles = "Tuner,Administrator")]
    public async Task<ActionResult> CompleteTune(
        string productionCode,
        [FromBody] CompleteBowlDimpleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlTuneCommand(productionCode, request.Duration),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/glue/complete")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> CompleteGlue(
        string productionCode,
        [FromBody] CompleteBowlGlueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlGlueCommand(
                productionCode,
                request.PairedProductionCode,
                request.ScaleId),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/glue-room/release")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> ReleaseFromGlueRoom(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReleaseBowlFromGlueRoomCommand(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/final-tune/complete")]
    [Authorize(Roles = "FineTuner,Administrator")]
    public async Task<ActionResult> CompleteFinalTune(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteHandpanFinalTuneCommand(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/qc/complete")]
    [Authorize(Roles = "QualityControl,Administrator")]
    public async Task<ActionResult> CompleteQualityControl(
        string productionCode,
        [FromBody] CompleteHandpanQualityControlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteHandpanQualityControlCommand(
                productionCode,
                request.Approved,
                request.RejectionReason,
                request.Details),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/packaging/complete")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> CompletePackaging(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteHandpanPackagingCommand(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
