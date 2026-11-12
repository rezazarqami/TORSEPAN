using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Scales;
using TORSEPAN.Infrastructure.Persistence;

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

    [HttpGet("legacy-top-bowls")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<ActionResult<IReadOnlyList<LegacyTopBowlScaleDto>>> GetLegacyTopBowls(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetLegacyTopBowlScalesQuery(), cancellationToken));

    [HttpGet("{id:guid}/photos")]
    public async Task<IActionResult> GetPhotos(Guid id, [FromServices] TORSEPANDbContext db, CancellationToken cancellationToken)
    {
        var photos = await db.ScalePhotos.AsNoTracking().SingleOrDefaultAsync(x => x.ScaleId == id, cancellationToken);
        return Ok(new { Top = ImageSource(photos?.Top), Bottom = ImageSource(photos?.Bottom) });
    }

    [HttpPost, RequestSizeLimit(14_000_000)]
    public async Task<ActionResult<Guid>> Create(CreateScaleCommand command, CancellationToken cancellationToken)
    {
        if (!ValidPhoto(command.TopPhoto) || !ValidPhoto(command.BottomPhoto))
            return BadRequest(PhotoError);
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(id);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int usage, CancellationToken cancellationToken)
    { await _mediator.Send(new DeactivateScaleCommand(id, usage), cancellationToken); return NoContent(); }

    [HttpPut("{id:guid}"), RequestSizeLimit(14_000_000)]
    public async Task<IActionResult> Rename(Guid id, RenameScaleRequest request, CancellationToken cancellationToken)
    {
        if (!ValidPhoto(request.TopPhoto) || !ValidPhoto(request.BottomPhoto))
            return BadRequest(PhotoError);
        await _mediator.Send(new RenameScaleCommand(id, request.Name, request.TopPhoto, request.BottomPhoto), cancellationToken);
        return NoContent();
    }

    private const string PhotoError = "عکس باید JPEG، PNG یا WebP و حداکثر ۵ مگابایت باشد.";

    private static bool ValidPhoto(byte[]? photo) =>
        photo is null || (photo.Length <= 5_000_000 && ImageType(photo) is not null);

    private static string? ImageSource(byte[]? photo) =>
        photo is not null && ImageType(photo) is { } type ? $"data:{type};base64,{Convert.ToBase64String(photo)}" : null;

    private static string? ImageType(byte[] photo)
    {
        if (HandpanPhotosController.IsImage(photo, photo.Length, "image/jpeg")) return "image/jpeg";
        if (HandpanPhotosController.IsImage(photo, photo.Length, "image/webp")) return "image/webp";

        ReadOnlySpan<byte> pngHeader = [137, 80, 78, 71, 13, 10, 26, 10];
        ReadOnlySpan<byte> pngEnd = [0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130];
        return photo.Length >= 45 && photo.AsSpan().StartsWith(pngHeader) && photo.AsSpan().EndsWith(pngEnd)
            ? "image/png"
            : null;
    }
}

public sealed record RenameScaleRequest(string Name, byte[]? TopPhoto = null, byte[]? BottomPhoto = null);
