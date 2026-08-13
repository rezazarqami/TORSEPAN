using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/payroll"), Authorize(Roles = "Administrator,ProductionManager")]
public sealed class PayrollController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var now = DateTime.UtcNow.AddHours(3.5);
        var pc = new PersianCalendar();
        var start = from?.Date ?? pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
        var end = to?.Date.AddDays(1) ?? now.AddDays(1).Date;
        var startUtc = DateTime.SpecifyKind(start.AddHours(-3.5), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.AddHours(-3.5), DateTimeKind.Utc);

        var events = await db.ProductionEvents.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Bowl)!.ThenInclude(x => x.Material)
            .Include(x => x.Assembly)!.ThenInclude(x => x.TopBowl).ThenInclude(x => x.Material)
            .Include(x => x.Handpan)!.ThenInclude(x => x.Assembly).ThenInclude(x => x.TopBowl).ThenInclude(x => x.Material)
            .Where(x => x.EventDate >= startUtc && x.EventDate < endUtc && x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") &&
                (x.Action == ProductionAction.Dimple || x.Action == ProductionAction.Shape || x.Action == ProductionAction.Glue || x.Action == ProductionAction.Tune || x.Action == ProductionAction.FineTune))
            .ToListAsync(ct);

        var rates = await db.PayrollRates.AsNoTracking().Include(x => x.Material).ToListAsync(ct);
        var lines = events.GroupBy(x => new
        {
            x.UserId, x.User.FullName, x.User.UserName, x.User.DisplayOrder, x.Action,
            MaterialId = x.Bowl != null ? x.Bowl.MaterialId : x.Assembly != null ? x.Assembly.TopBowl.MaterialId : x.Handpan != null ? x.Handpan.Assembly.TopBowl.MaterialId : (Guid?)null,
            Material = x.Bowl != null ? x.Bowl.Material.Name : x.Assembly != null ? x.Assembly.TopBowl.Material.Name : x.Handpan != null ? x.Handpan.Assembly.TopBowl.Material.Name : "—",
            BowlType = x.Bowl == null ? (int?)null : (int)x.Bowl.BowlType
        }).Select(g =>
        {
            var rate = rates.Where(r => r.Action == g.Key.Action && (!r.MaterialId.HasValue || r.MaterialId == g.Key.MaterialId) && (!r.BowlType.HasValue || (int)r.BowlType == g.Key.BowlType))
                .OrderByDescending(r => r.MaterialId.HasValue).ThenByDescending(r => r.BowlType.HasValue).FirstOrDefault()?.Amount ?? 0;
            return new PayrollLine(g.Key.UserId, string.IsNullOrWhiteSpace(g.Key.FullName) ? g.Key.UserName : g.Key.FullName, g.Key.DisplayOrder, (int)g.Key.Action, Title(g.Key.Action), g.Key.MaterialId, g.Key.Material, g.Key.BowlType, g.Count(), rate, g.Count() * rate);
        }).ToList();

        var users = await db.Users.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.FullName).Select(x => new PayrollUser(x.Id, x.FullName, x.DisplayOrder)).ToListAsync(ct);
        return Ok(new { From = start, To = end.AddDays(-1), Lines = lines.OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName), Users = users, Rates = rates.Select(r => new PayrollRateDto(r.Id, (int)r.Action, Title(r.Action), r.MaterialId, r.Material?.Name ?? "همه متریال‌ها", r.BowlType.HasValue ? (int)r.BowlType : null, r.Amount)) });
    }

    [HttpPost("rates")]
    public async Task<IActionResult> SaveRate(PayrollRateRequest request, CancellationToken ct)
    {
        var action = (ProductionAction)request.Action;
        var bowlType = request.BowlType.HasValue ? (BowlType?)request.BowlType.Value : null;
        var rate = await db.PayrollRates.FirstOrDefaultAsync(x => x.Action == action && x.MaterialId == request.MaterialId && x.BowlType == bowlType, ct);
        if (rate is null) db.PayrollRates.Add(new PayrollRate(action, request.MaterialId, bowlType, request.Amount)); else rate.SetAmount(request.Amount);
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("users/order")]
    public async Task<IActionResult> SaveOrder(List<UserOrderRequest> items, CancellationToken ct)
    {
        var ids = items.Select(x => x.UserId).ToList();
        var users = await db.Users.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var user in users) user.SetDisplayOrder(items.First(x => x.UserId == user.Id).Order);
        await db.SaveChangesAsync(ct); return NoContent();
    }

    private static string Title(ProductionAction action) => action switch { ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ", ProductionAction.Glue => "چسب", ProductionAction.Tune => "تیون", ProductionAction.FineTune => "فاین تیون", _ => action.ToString() };
}

public sealed record PayrollLine(Guid UserId, string UserName, int DisplayOrder, int Action, string ActionTitle, Guid? MaterialId, string MaterialName, int? BowlType, int Count, decimal Rate, decimal Total);
public sealed record PayrollUser(Guid Id, string FullName, int DisplayOrder);
public sealed record PayrollRateDto(Guid Id, int Action, string ActionTitle, Guid? MaterialId, string MaterialName, int? BowlType, decimal Amount);
public sealed record PayrollRateRequest(int Action, Guid? MaterialId, int? BowlType, decimal Amount);
public sealed record UserOrderRequest(Guid UserId, int Order);
