using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/payroll-exclusions"), Authorize(Roles = "Administrator,ProductionManager")]
public sealed class PayrollExclusionsController(TORSEPANDbContext db) : ControllerBase
{
    private static readonly ProductionAction[] Allowed = [ProductionAction.Dimple, ProductionAction.Shape, ProductionAction.Tune, ProductionAction.Glue, ProductionAction.Design, ProductionAction.FineTune];

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string code, CancellationToken ct)
    {
        code = (code ?? string.Empty).Trim();
        if (code.Length == 0) return BadRequest("کد ساز یا کاسه را وارد کنید.");
        var bowl = await db.Bowls.AsNoTracking().FirstOrDefaultAsync(x => x.ProductionCode == code, ct);
        var handpan = await db.Handpans.AsNoTracking().Include(x => x.Assembly)
            .FirstOrDefaultAsync(x => x.SerialNumber == code || (bowl != null && (x.Assembly.TopBowlId == bowl.Id || x.Assembly.BottomBowlId == bowl.Id)), ct);
        if (bowl is null && handpan is null) return NotFound("کدی با این مشخصات پیدا نشد.");
        var bowlIds = handpan is null ? new[] { bowl!.Id } : new[] { handpan.Assembly.TopBowlId, handpan.Assembly.BottomBowlId };
        var assemblyId = handpan?.AssemblyId;
        var handpanId = handpan?.Id;
        var events = await db.ProductionEvents.AsNoTracking().Include(x => x.User).Include(x => x.Bowl)
            .Where(x => x.Result == EventResult.Completed && Allowed.Contains(x.Action) &&
                ((x.BowlId.HasValue && bowlIds.Contains(x.BowlId.Value)) ||
                 (assemblyId.HasValue && x.AssemblyId == assemblyId) ||
                 (handpanId.HasValue && x.HandpanId == handpanId)))
            .OrderBy(x => x.EventDate).Select(x => new { x.Id, Action = x.Action.ToString(), ActionTitle = Title(x.Action), BowlCode = x.Bowl != null ? x.Bowl.ProductionCode : null, Performer = x.User.FullName, x.EventDate, x.IsPayrollExcluded }).ToListAsync(ct);
        return Ok(new { Code = handpan?.SerialNumber ?? bowl!.ProductionCode, Events = events });
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] PayrollExclusionSave request, CancellationToken ct)
    {
        var ids = request.EventIds.Distinct().ToArray();
        var events = await db.ProductionEvents.Where(x => ids.Contains(x.Id) && Allowed.Contains(x.Action) && x.Result == EventResult.Completed).ToListAsync(ct);
        if (events.Count != ids.Length) return BadRequest("یک یا چند عملیات معتبر نیست.");
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        foreach (var item in events) item.SetPayrollExcluded(request.Excluded, userId);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string Title(ProductionAction action) => action switch { ProductionAction.Dimple => "Dimple", ProductionAction.Shape => "Shape", ProductionAction.Tune => "Tune", ProductionAction.Glue => "چسب", ProductionAction.Design => "Design", ProductionAction.FineTune => "Fine Tune", _ => action.ToString() };
}
public sealed record PayrollExclusionSave(IReadOnlyCollection<Guid> EventIds, bool Excluded);
