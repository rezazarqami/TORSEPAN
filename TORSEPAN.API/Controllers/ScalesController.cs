using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Scales;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ScalesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ScalesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScaleDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAllScalesQuery(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<ActionResult<Guid>> Create(CreateScaleCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(id);
    }
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int usage, CancellationToken cancellationToken)
    { await _mediator.Send(new DeactivateScaleCommand(id, usage), cancellationToken); return NoContent(); }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Rename(Guid id, RenameScaleRequest request, CancellationToken cancellationToken)
    { await _mediator.Send(new RenameScaleCommand(id, request.Name), cancellationToken); return NoContent(); }
}

public sealed record RenameScaleRequest(string Name);
