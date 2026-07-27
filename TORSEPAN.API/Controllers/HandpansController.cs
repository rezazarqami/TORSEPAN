using MediatR;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Handpans.Commands.CreateHandpan;
using TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber;
using TORSEPAN.Application.Handpans.Queries.GetHandpansByStatus;
using TORSEPAN.Application.Handpans.Queries.GetReadyForPackaging;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HandpansController : ControllerBase
{
    private readonly IMediator _mediator;

    public HandpansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{serialNumber}")]
    public async Task<ActionResult<TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber.HandpanDto>>
        GetBySerialNumber(
            string serialNumber,
            CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetHandpanBySerialNumberQuery(serialNumber),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IReadOnlyList<TORSEPAN.Application.Handpans.Queries.GetHandpansByStatus.HandpanDto>>>
        GetByStatus(
            ProductionStatus status,
            CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetHandpansByStatusQuery(status),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("ready-for-packaging")]
    public async Task<ActionResult<IReadOnlyList<TORSEPAN.Application.Handpans.Queries.GetReadyForPackaging.HandpanDto>>>
        GetReadyForPackaging(
            CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetReadyForPackagingQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateHandpanCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetBySerialNumber),
            new
            {
                serialNumber = command.SerialNumber
            },
            id);
    }
}