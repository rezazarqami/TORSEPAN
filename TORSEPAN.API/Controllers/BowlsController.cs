using MediatR;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Common.Extensions;
using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Common.Pagination;
using TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BowlsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BowlsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BowlDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BowlDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllBowlsQuery(
            new PageRequest(page, pageSize));

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BowlDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateBowlCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            id);
    }
}