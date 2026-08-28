using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/handpans/{handpanId:guid}/photos"), Authorize]
public sealed class HandpanPhotosController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid handpanId, CancellationToken ct) => Ok(await db.HandpanPhotos.AsNoTracking()
        .Where(x => x.HandpanId == handpanId).OrderBy(x => x.CreatedAt)
        .Select(x => new { x.Id, x.CreatedAt }).ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Image(Guid handpanId, Guid id, [FromQuery] bool thumbnail, CancellationToken ct)
    {
        var photo = await db.HandpanPhotos.AsNoTracking().Where(x => x.HandpanId == handpanId && x.Id == id)
            .Select(x => new { Data = thumbnail ? x.Thumbnail : x.Image, x.ContentType }).SingleOrDefaultAsync(ct);
        return photo is null ? NotFound() : File(photo.Data, photo.ContentType);
    }

    [HttpPost, RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Upload(Guid handpanId, IFormFile file, IFormFile thumbnail, CancellationToken ct)
    {
        var handpan = await db.Handpans.SingleOrDefaultAsync(x => x.Id == handpanId, ct);
        if (handpan is null) return NotFound();
        if (handpan.Stage < ProductionStage.WaitingForFinalTune) return Conflict("آپلود عکس پس از خروج ساز از اتاق چسب فعال می‌شود.");
        if (file.Length is <= 0 or > 5_000_000 || thumbnail.Length is <= 0 or > 500_000) return BadRequest("حجم عکس بهینه‌شده معتبر نیست.");
        if (file.ContentType != "image/webp" || thumbnail.ContentType != "image/webp") return BadRequest("فرمت عکس باید WebP باشد.");
        await using var imageStream = new MemoryStream(); await using var thumbStream = new MemoryStream();
        await file.CopyToAsync(imageStream, ct); await thumbnail.CopyToAsync(thumbStream, ct);
        if (!IsWebp(imageStream.GetBuffer(), imageStream.Length) || !IsWebp(thumbStream.GetBuffer(), thumbStream.Length)) return BadRequest("محتوای عکس معتبر نیست.");
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var item = new HandpanPhoto(handpanId, imageStream.ToArray(), thumbStream.ToArray(), "image/webp", userId);
        db.HandpanPhotos.Add(item); await db.SaveChangesAsync(ct);
        return Ok(new { item.Id, item.CreatedAt });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid handpanId, Guid id, CancellationToken ct)
    {
        var item = await db.HandpanPhotos.SingleOrDefaultAsync(x => x.HandpanId == handpanId && x.Id == id, ct);
        if (item is null) return NotFound(); db.HandpanPhotos.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
    private static bool IsWebp(byte[] data, long length) => length >= 12 && data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F' && data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P';
}
