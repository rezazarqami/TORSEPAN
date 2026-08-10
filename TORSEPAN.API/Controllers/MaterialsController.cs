using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Materials.Commands.CreateMaterial;
using TORSEPAN.Application.Materials.Commands.DeleteMaterial;
using TORSEPAN.Application.Materials.Commands.UpdateMaterial;
using TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;
using TORSEPAN.Application.Materials.Commands.AdjustBowlStock;
using TORSEPAN.Application.Materials.Queries.GetAllMaterials;
using TORSEPAN.Application.Materials.Queries.GetMaterialById;

using GetAllMaterialDto = TORSEPAN.Application.Materials.Queries.GetAllMaterials.MaterialDto;
using GetByIdMaterialDto = TORSEPAN.Application.Materials.Queries.GetMaterialById.MaterialDto;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MaterialsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MaterialsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<GetAllMaterialDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllMaterialsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetByIdMaterialDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMaterialByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateMaterialCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMaterialCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest();

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteMaterialCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<int>> AdjustStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 0 || (!request.SetAbsolute && request.Quantity == 0))
            return BadRequest("Quantity is invalid.");

        try
        {
            return Ok(await _mediator.Send(
                new AdjustMaterialStockCommand(id, request.Quantity, request.SetAbsolute),
                cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{id:guid}/bowl-stock")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> AdjustBowlStock(Guid id, [FromBody] AdjustBowlStockRequest request, CancellationToken cancellationToken)
    {
        if (request.TopQuantity < 0 || request.BottomQuantity < 0 ||
            (!request.SetAbsolute && request.TopQuantity == 0 && request.BottomQuantity == 0))
            return BadRequest("Quantities are invalid.");

        try
        {
            await _mediator.Send(new AdjustBowlStockCommand(
                id, request.TopQuantity, request.BottomQuantity, request.SetAbsolute), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException) { return BadRequest(); }
    }
}

public sealed record AdjustStockRequest(int Quantity, bool SetAbsolute = false);
public sealed record AdjustBowlStockRequest(int TopQuantity, int BottomQuantity, bool SetAbsolute = false);
