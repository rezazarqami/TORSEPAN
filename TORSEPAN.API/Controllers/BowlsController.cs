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
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application;
using TORSEPAN.Application.Bowls.Queries.GetExportWarehouse;
using TORSEPAN.Application.Sales;

namespace TORSEPAN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BowlsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductionDeletionService _deletionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionRollbackService _rollbackService;

    public BowlsController(IMediator mediator, IProductionDeletionService deletionService, IUnitOfWork unitOfWork, IProductionRollbackService rollbackService)
    {
        _mediator = mediator;
        _deletionService = deletionService;
        _unitOfWork = unitOfWork;
        _rollbackService = rollbackService;
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await _deletionService.DeleteBowlAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/rollback")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Rollback(Guid id, CancellationToken cancellationToken)
        => await _rollbackService.RollbackBowlAsync(id, cancellationToken) ? NoContent() : BadRequest();

    [HttpGet]
    public async Task<ActionResult<PagedResult<BowlDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? bowlType = null, [FromQuery] bool? hasNotes = null,
        [FromQuery] Guid? materialId = null, [FromQuery] Guid? scaleId = null,
        [FromQuery] int? stage = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAllBowlsQuery(new PageRequest(page, pageSize), bowlType, hasNotes, materialId, scaleId, stage),
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
        var canOverrideCode = User.IsInRole("Administrator") || User.IsInRole("ProductionManager");
        if (!canOverrideCode)
        {
            var suggestion = await BuildSuggestedCode(command.MaterialId, (int)command.BowlType, cancellationToken);
            if (!string.Equals(
                    ProductionCodeNormalizer.Normalize(command.ProductionCode),
                    ProductionCodeNormalizer.Normalize(suggestion.SuggestedCode),
                    StringComparison.OrdinalIgnoreCase))
                return BadRequest($"ثبت کاسه فقط با کد پیشنهادی «{suggestion.SuggestedCode}» مجاز است.");
        }

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
    [Authorize(Roles = "Dimpler,Shaper,Workshop,Tuner,FineTuner,QualityControl,ProductionManager,Administrator")]
    public async Task<ActionResult> GetForDimpling(
        string productionCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBowlForDimpleQuery(productionCode),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("production/shapers")]
    [Authorize(Roles = "Shaper,Administrator")]
    public async Task<IActionResult> GetShapers()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(users.Where(x => x.IsActive && x.UserRoles.Any(r => r.Role.Name == "Shaper"))
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.UserName, x.FullName }));
    }

    [HttpGet("suggested-code")]
    public async Task<IActionResult> SuggestedCode([FromQuery] Guid? materialId, [FromQuery] int? bowlType, CancellationToken cancellationToken)
    {
        return Ok(await BuildSuggestedCode(materialId, bowlType, cancellationToken));
    }

    private async Task<SuggestedBowlCode> BuildSuggestedCode(Guid? materialId, int? bowlType, CancellationToken cancellationToken)
    {
        var bowls = await _unitOfWork.Bowls.GetAllAsync(cancellationToken);
        if (!materialId.HasValue || !bowlType.HasValue)
            return new SuggestedBowlCode("—", string.Empty, string.Empty);

        var material = await _unitOfWork.Materials.GetByIdAsync(materialId.Value);
        var template = ProductionCodeNormalizer.Normalize(
            bowlType.Value == 1 ? material?.TopBowlCodeTemplate : material?.BottomBowlCodeTemplate)
            .ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(template))
            return new SuggestedBowlCode("—", string.Empty, string.Empty);

        var digits = template.Reverse().TakeWhile(x => x == '0').Count();
        if (digits == 0)
            return new SuggestedBowlCode("—", string.Empty, template);

        var prefix = template[..^digits];
        var matching = bowls
            .Where(x => x.MaterialId == materialId.Value && (int)x.BowlType == bowlType.Value)
            .Select(x => ProductionCodeNormalizer.Normalize(x.ProductionCode).ToUpperInvariant())
            .Where(x => x.StartsWith(prefix, StringComparison.Ordinal) && x.Length == prefix.Length + digits)
            .Select(x => new { Code = x, Number = int.TryParse(x[prefix.Length..], out var number) ? number : -1 })
            .Where(x => x.Number >= 0)
            .OrderByDescending(x => x.Number)
            .ToList();
        var last = matching.FirstOrDefault();
        var next = (last?.Number ?? 0) + 1;
        return new SuggestedBowlCode(last?.Code ?? "—", $"{prefix}{next.ToString($"D{digits}")}", template);
    }

    private sealed record SuggestedBowlCode(string LastCode, string SuggestedCode, string Template);

    [HttpPost("production/{productionCode}/notes")]
    [Authorize(Roles = "Dimpler,Shaper,Workshop,Tuner,FineTuner,QualityControl,ProductionManager,Administrator")]
    public async Task<ActionResult> AddNote(string productionCode, [FromBody] ProductionNoteRequest request,
        CancellationToken cancellationToken)
        => this.ToActionResult(await _mediator.Send(
            new AddProductionNoteCommand(productionCode, request.Description ?? string.Empty, request.IsInstrumentNote), cancellationToken));

    [HttpPost("dimpling/{productionCode}/complete")]
    [Authorize(Roles = "Dimpler,Shaper,Administrator")]
    public async Task<ActionResult> CompleteDimpling(
        string productionCode,
        [FromBody] CompleteBowlDimpleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlDimpleCommand(productionCode, request.Duration, request.ScaleId),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("production/{productionCode}/shape/complete")]
    [Authorize(Roles = "Shaper,Administrator")]
    public async Task<ActionResult> CompleteShape(
        string productionCode,
        [FromBody] CompleteShapeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteBowlShapeCommand(productionCode, request.Duration, request.StretchUserId, request.NoteAreaUserId, request.EditUserId),
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

    [HttpPost("production/{productionCode}/tune/export")]
    [Authorize(Roles = "Tuner,Administrator")]
    public async Task<ActionResult> CompleteTuneForExport(string productionCode,
        [FromBody] CompleteBowlDimpleRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await _mediator.Send(
            new CompleteBowlTuneForExportCommand(productionCode, request.Duration), cancellationToken));

    [HttpPost("production/{productionCode}/export-packaging/complete")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> CompleteExportPackaging(string productionCode, CancellationToken cancellationToken)
        => this.ToActionResult(await _mediator.Send(
            new CompleteExportPackagingCommand(productionCode), cancellationToken));

    [HttpPost("production/{productionCode}/export-packaging/send-to-glue")]
    [Authorize(Roles = "Workshop,Administrator")]
    public async Task<ActionResult> ReturnExportBowlToGlue(string productionCode, CancellationToken cancellationToken)
        => this.ToActionResult(await _mediator.Send(new ReturnExportBowlToGlueCommand(productionCode), cancellationToken));

    [HttpGet("export-warehouse")]
    public async Task<IActionResult> ExportWarehouse(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetExportWarehouseQuery(), cancellationToken));

    [HttpPost("export-warehouse/{id:guid}/ship")]
    [Authorize(Roles = "Workshop,Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> ShipExportBowl(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ShipExportBowlCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("export-warehouse/ship")]
    [Authorize(Roles = "Workshop,Administrator,ProductionManager,SalesAdmin")]
    public async Task<IActionResult> ShipExportBowls([FromBody] ExportShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var method = string.IsNullOrWhiteSpace(request.ShippingMethod) ? null : request.ShippingMethod.Trim().ToLowerInvariant();
        if (method is not null && method is not ("air" or "land" or "sea"))
            return BadRequest("روش ارسال معتبر نیست.");
        await _mediator.Send(new ShipExportBowlsCommand(request.BowlIds ?? [], request.BuyerName,
            request.PartyId, request.Destination, method, request.IsSettled), cancellationToken);
        return NoContent();
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
        [FromBody] CompleteBowlDimpleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteHandpanFinalTuneCommand(productionCode, request.Duration),
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
        [FromBody] CompletePackagingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteHandpanPackagingCommand(productionCode, request.MaterialIds ?? []),
            cancellationToken);

        return this.ToActionResult(result);
    }
}

public sealed record ProductionNoteRequest(string? Description, bool IsInstrumentNote = false);
public sealed record CompleteShapeRequest(TORSEPAN.Domain.Enums.OperationDuration Duration, Guid? StretchUserId, Guid? NoteAreaUserId, Guid? EditUserId);
public sealed record ExportShipmentRequest(IReadOnlyCollection<Guid>? BowlIds, string? BuyerName,
    Guid? PartyId, string? Destination, string? ShippingMethod, bool? IsSettled);
