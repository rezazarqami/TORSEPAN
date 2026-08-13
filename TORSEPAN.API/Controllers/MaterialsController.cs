Exit code: 0
Wall time: 0.6 seconds
Output:
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Materials.Commands.CreateMaterial;
using TORSEPAN.Application.Materials.Commands.DeleteMaterial;
using TORSEPAN.Application.Materials.Commands.UpdateMaterial;
using TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;
using TORSEPAN.Application.Materials.Commands.AdjustBowlStock;
using TORSEPAN.Application.Materials.Commands.SetLowStockThreshold;
using TORSEPAN.Application.Materials.Queries.GetAllMaterials;
using TORSEPAN.Application.Materials.Queries.GetMaterialById;
using TORSEPAN.Application.Interfaces;
using System.Text.RegularExpressions;

using GetAllMaterialDto = TORSEPAN.Application.Materials.Queries.GetAllMaterials.MaterialDto;
using GetByIdMaterialDto = TORSEPAN.Application.Materials.Queries.GetMaterialById.MaterialDto;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MaterialsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public MaterialsController(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
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

    [HttpPatch("{id:guid}/low-stock-threshold")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetThreshold(Guid id, [FromBody] LowStockThresholdRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 0 || request.TopQuantity < 0 || request.BottomQuantity < 0) return BadRequest();
        await _mediator.Send(new SetLowStockThresholdCommand(id, request.Quantity, request.TopQuantity, request.BottomQuantity), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/bowl-code-templates")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetBowlCodeTemplates(Guid id, [FromBody] BowlCodeTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        static bool Valid(string? value) => string.IsNullOrWhiteSpace(value) ||
            Regex.IsMatch(value.Trim(), "^[A-Za-z]{2}-0{5}$");
        if (!Valid(request.TopTemplate) || !Valid(request.BottomTemplate))
            return BadRequest("Template must look like AB-00000.");
        var material = await _unitOfWork.Materials.GetByIdAsync(id);
        if (material is null) return NotFound();
        material.SetBowlCodeTemplates(request.TopTemplate, request.BottomTemplate);
        _unitOfWork.Materials.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record AdjustStockRequest(int Quantity, bool SetAbsolute = false);
public sealed record AdjustBowlStockRequest(int TopQuantity, int BottomQuantity, bool SetAbsolute = false);
public sealed record LowStockThresholdRequest(int Quantity, int TopQuantity, int BottomQuantity);
public sealed record BowlCodeTemplatesRequest(string? TopTemplate, string? BottomTemplate);

