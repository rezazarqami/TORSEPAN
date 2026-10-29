using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/work-plans"), Authorize]
public sealed class WorkPlansController(TORSEPANDbContext db) : ControllerBase
{
    public sealed record TaskLine(string Key, string Label, int Target);
    public sealed record SaveRow(Guid UserId, Guid? Revision, List<TaskLine> Tasks, string Notes);
    public sealed record SaveRequest(DateTime Date, bool IsException, List<SaveRow> Users);
    private static DateTime Today => DateTime.UtcNow.AddHours(3.5).Date;
    private static bool ValidDate(DateTime date) => date.Year >= 1900 && date.Year < 9998;
    private static string[] Roles(User user) => user.UserRoles.Select(x => x.Role.Name).Append(user.Role).Distinct().ToArray();
    public static List<TaskLine> Options(IEnumerable<string> roles)
    {
        var result = new List<TaskLine>();
        foreach (var role in roles.Distinct())
        {
            var action = role switch { "Dimpler" => ProductionAction.Dimple, "Shaper" => ProductionAction.Shape,
                "Tuner" => ProductionAction.Tune, "FineTuner" => ProductionAction.FineTune,
                "QualityControl" => ProductionAction.QualityCheck, _ => (ProductionAction?)null };
            if (action is null) continue;
            var label = role switch { "Dimpler" => "دیمپل", "Shaper" => "شیپ", "Tuner" => "تیون", "FineTuner" => "فاین‌تیون", _ => "کنترل کیفیت" };
            if (role is "Dimpler" or "Shaper" or "Tuner")
            {
                result.Add(new($"{(int)action}:1", label + " کاسه رو", 0));
                result.Add(new($"{(int)action}:2", label + " کاسه زیر", 0));
            }
            else result.Add(new($"{(int)action}:0", label, 0));
        }
        return result;
    }
    public static WorkPlan? Resolve(IEnumerable<WorkPlan> plans, Guid userId, DateTime day, bool recurringOnly = false) =>
        plans.Where(x => x.UserId == userId && (recurringOnly ? !x.IsException && x.EffectiveDate <= day :
            x.IsException ? x.EffectiveDate == day : x.EffectiveDate <= day))
            .OrderByDescending(x => !recurringOnly && x.IsException).ThenByDescending(x => x.EffectiveDate).FirstOrDefault();

    [HttpGet, Authorize(Roles = "Administrator,ProductionManager")]
    public Task<IActionResult> Get(DateTime? date, bool recurringOnly = false, CancellationToken ct = default) => Read(date ?? Today, null, recurringOnly, ct);

    [HttpGet("mine")]
    public Task<IActionResult> Mine(DateTime? date, CancellationToken ct = default) =>
        Read(date ?? Today, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), false, ct);

    private async Task<IActionResult> Read(DateTime day, Guid? ownId, bool recurringOnly, CancellationToken ct)
    {
        day = day.Date;
        if (!ValidDate(day)) return BadRequest(new { Message = "تاریخ معتبر انتخاب کنید." });
        var users = await db.Users.AsNoTracking().Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Where(x => !x.IsDeleted && (!ownId.HasValue || x.Id == ownId)).OrderBy(x => x.DisplayOrder).ThenBy(x => x.FullName).ToListAsync(ct);
        users = users.Where(x => !Roles(x).Contains("ProductionManager")).ToList();
        var ids = users.Select(x => x.Id).ToArray();
        var plans = await db.WorkPlans.AsNoTracking().Where(x => ids.Contains(x.UserId) && x.EffectiveDate <= day).ToListAsync(ct);
        var start = DateTime.SpecifyKind(day.AddHours(-3.5), DateTimeKind.Utc);
        var end = start.AddDays(1);
        var actual = await db.ProductionEvents.AsNoTracking().Where(x => ids.Contains(x.UserId) && x.EventDate >= start && x.EventDate < end
            && x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") && !x.Description.StartsWith("LEGACY_STOCK:") && x.Description != "Released from glue room")
            .GroupBy(x => new { x.UserId, x.Action, BowlType = x.Bowl != null ? (int)x.Bowl.BowlType : 0 })
            .Select(g => new { g.Key.UserId, g.Key.Action, g.Key.BowlType, Count = g.Count() }).ToListAsync(ct);
        var rows = users.Select(u => {
            var plan = Resolve(plans, u.Id, day, recurringOnly);
            var tasks = plan is null ? Options(Roles(u)) : JsonSerializer.Deserialize<List<TaskLine>>(plan.TasksJson)!;
            return new { UserId = u.Id, u.FullName, u.Title, u.IsActive, Roles = Roles(u), HasPlan = plan != null,
                plan?.Revision, EffectiveDate = plan?.EffectiveDate, IsException = plan?.IsException ?? false, Notes = plan?.Notes ?? "",
                Options = Options(Roles(u)), Tasks = tasks.Select(t => {
                    var parts = t.Key.Split(':'); var action = int.Parse(parts[0]); var bowl = int.Parse(parts[1]);
                    var count = actual.Where(a => a.UserId == u.Id && (int)a.Action == action && (bowl == 0 || a.BowlType == bowl)).Sum(a => a.Count);
                    return new { t.Key, t.Label, t.Target, Actual = count, Passed = plan != null && t.Target > 0 ? (bool?)(count >= t.Target) : null };
                }) };
        });
        return Ok(new { Date = day, Users = rows });
    }

    [HttpPut, Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> Save(SaveRequest request, CancellationToken ct)
    {
        if (!ValidDate(request.Date) || request.Users is null || request.Users.Count is < 1 or > 1000 || request.Users.Any(x => x is null) || request.Users.Select(x => x.UserId).Distinct().Count() != request.Users.Count)
            return BadRequest(new { Message = "تاریخ یا فهرست افراد معتبر نیست." });
        var date = request.Date.Date;
        var ids = request.Users.Select(x => x.UserId).ToArray();
        var users = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).Where(x => ids.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct);
        if (users.Count != ids.Length || users.Any(x => Roles(x).Contains("ProductionManager"))) return BadRequest(new { Message = "فهرست همکاران تغییر کرده؛ دوباره بارگذاری کنید." });
        var existing = await db.WorkPlans.Where(x => ids.Contains(x.UserId) && x.EffectiveDate <= date).ToListAsync(ct);
        foreach (var row in request.Users)
        {
            var options = Options(Roles(users.Single(x => x.Id == row.UserId)));
            if (row.Notes is null || row.Notes.Length > 2000 || row.Tasks is null || row.Tasks.Count != options.Count || row.Tasks.Any(t => t is null) ||
                row.Tasks.Select(x => x.Key).Distinct().Count() != row.Tasks.Count || row.Tasks.Any(t => t.Target < 0 || t.Target > 100000 || !options.Any(o => o.Key == t.Key)))
                return BadRequest(new { Message = "وظایف باید مطابق نقش فرد و تعدادها بین صفر تا ۱۰۰۰۰۰ باشند؛ توضیحات حداکثر ۲۰۰۰ نویسه است." });
            var current = Resolve(existing, row.UserId, date, !request.IsException);
            if (current?.Revision != row.Revision) return Conflict(new { Message = "برنامه هم‌زمان تغییر کرده؛ دوباره بارگذاری کنید." });
            var plan = existing.SingleOrDefault(x => x.UserId == row.UserId && x.EffectiveDate == date && x.IsException == request.IsException);
            if (plan is null) { plan = new() { UserId = row.UserId, EffectiveDate = date, IsException = request.IsException }; db.WorkPlans.Add(plan); }
            plan.TasksJson = JsonSerializer.Serialize(options.Select(o => o with { Target = row.Tasks.Single(t => t.Key == o.Key).Target }));
            plan.Notes = row.Notes.Trim(); plan.Revision = Guid.NewGuid();
            plan.UpdatedAt = DateTime.UtcNow; plan.UpdatedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { Message = "ذخیره انجام نشد؛ برنامه را دوباره بارگذاری کنید." }); }
        return Ok(new { Message = "برنامه ذخیره شد." });
    }
}
