using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Common.Extensions;
using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Common.Pagination;
using TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

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
}