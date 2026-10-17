using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.API.Offline;
namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/offline"), Authorize(Roles = "Administrator")]
public sealed class OfflineController(OfflineProductionService service) : ControllerBase
{
    private Guid Owner => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("access/{deviceId:guid}")]
    public Task<IActionResult> Access(Guid deviceId, CancellationToken ct) => Execute(async () => {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.AccessAsync(Owner, deviceId, ct));
    });
    [HttpPost("enroll")]
    public Task<IActionResult> Enroll(OfflineEnrollment request, CancellationToken ct) => Execute(async () => {
        await service.EnrollAsync(Owner, request.DeviceId, ct); return Ok(new { enabled = true });
    });
    [HttpGet("snapshot/{deviceId:guid}")]
    public Task<IActionResult> Snapshot(Guid deviceId, CancellationToken ct) => Execute(async () => {
        Response.Headers.CacheControl = "no-store"; return Ok(await service.SnapshotAsync(Owner, deviceId, ct));
    });
    [HttpPost("operations"), RequestSizeLimit(262144)]
    public Task<IActionResult> Apply(OfflineOperation request, CancellationToken ct) => Execute(async () => {
        if (request.Action == "photo" || request.Image is not null || request.Thumbnail is not null)
            throw new OfflineProblem("route", "عکس باید از مسیر مخصوص ارسال شود.",400);
        return Ok(await service.ApplyAsync(Owner, request, ct));
    });
    [HttpPost("photos"), RequestSizeLimit(7_500_000)]
    public Task<IActionResult> Photo(OfflineOperation request, CancellationToken ct) => Execute(async () => {
        if (request.Action != "photo") throw new OfflineProblem("route", "مسیر عکس معتبر نیست.",400);
        return Ok(await service.ApplyAsync(Owner, request, ct));
    });
    private async Task<IActionResult> Execute(Func<Task<IActionResult>> operation)
    {
        try { return await operation(); }
        catch (OfflineProblem error) { return StatusCode(error.Status, new { error.Code, Message = error.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { Code = "conflict", Message = "اطلاعات هم‌زمان تغییر کرده؛ صف محلی حفظ شد." }); }
        catch (Exception ex) when (ex is PostgresException { SqlState: "40001" or "23505" } || ex.InnerException is PostgresException { SqlState: "40001" or "23505" })
        { return StatusCode(503, new { Code = "retry", Message = "ثبت هم‌زمان رخ داد؛ عملیات با همان شناسه دوباره ارسال شود." }); }
    }
}
