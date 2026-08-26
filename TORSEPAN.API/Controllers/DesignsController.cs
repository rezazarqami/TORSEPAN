using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Application;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/designs"), Authorize]
public sealed class DesignsController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet("types")]
    public async Task<IActionResult> Types(CancellationToken ct) => Ok(await db.DesignTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Rate }).ToListAsync(ct));

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) => Ok(await db.Users.AsNoTracking().Where(x => x.IsActive)
        .OrderBy(x => x.DisplayOrder).ThenBy(x => x.FullName).Select(x => new { x.Id, Name = x.FullName == "" ? x.UserName : x.FullName }).ToListAsync(ct));

    [HttpPost("types"), Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> AddType(DesignTypeRequest request, CancellationToken ct)
    {
        var name=request.Name?.Trim()??""; if(name.Length==0)return BadRequest("نام دیزاین الزامی است.");
        if(await db.DesignTypes.AnyAsync(x=>x.Name.ToLower()==name.ToLower()&&x.IsActive,ct))return Conflict("این نوع دیزاین قبلاً ثبت شده است.");
        var item=new DesignType(name);item.SetRate(request.Rate);db.DesignTypes.Add(item);await db.SaveChangesAsync(ct);return Ok(new{id=item.Id,item.Name,item.Rate});
    }

    [HttpPut("types/{id:guid}/rate"), Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> SetRate(Guid id, DesignRateRequest request, CancellationToken ct)
    { var item=await db.DesignTypes.FirstOrDefaultAsync(x=>x.Id==id&&x.IsActive,ct);if(item is null)return NotFound();item.SetRate(request.Rate);await db.SaveChangesAsync(ct);return NoContent(); }

    [HttpDelete("types/{id:guid}"), Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> DeleteType(Guid id,CancellationToken ct){var item=await db.DesignTypes.FirstOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return NotFound();item.Deactivate();await db.SaveChangesAsync(ct);return NoContent();}

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDesignRequest request,CancellationToken ct)
    {
        var code=ProductionCodeNormalizer.Normalize(request.ProductionCode??"");
        var bowl=await db.Bowls.FirstOrDefaultAsync(x=>x.ProductionCode==code,ct);if(bowl is null)return NotFound("کاسه یا ساز پیدا نشد.");
        if(request.Items is null||request.Items.Count==0)return BadRequest("حداقل یک دیزاین انتخاب کنید.");
        var typeIds=request.Items.Select(x=>x.DesignTypeId).Distinct().ToList();
        var types=await db.DesignTypes.AsNoTracking().Where(x=>typeIds.Contains(x.Id)&&x.IsActive).ToDictionaryAsync(x=>x.Id,ct);
        if(types.Count!=typeIds.Count)return BadRequest("نوع دیزاین معتبر نیست.");
        var assembly=await db.HandpanAssemblies.FirstOrDefaultAsync(x=>x.TopBowlId==bowl.Id||x.BottomBowlId==bowl.Id,ct);
        var handpan=assembly is null?null:await db.Handpans.FirstOrDefaultAsync(x=>x.AssemblyId==assembly.Id,ct);
        var currentUserId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??throw new UnauthorizedAccessException());
        var requestedUsers=request.Items.Where(x=>x.UserId.HasValue).Select(x=>x.UserId!.Value).Distinct().ToList();
        var validUsers=(await db.Users.AsNoTracking().Where(x=>requestedUsers.Contains(x.Id)&&x.IsActive).Select(x=>x.Id).ToListAsync(ct)).ToHashSet();
        if(validUsers.Count!=requestedUsers.Count)return BadRequest("مجری انتخاب‌شده معتبر نیست.");
        var existingDescriptions=await db.ProductionEvents.AsNoTracking().Where(x=>x.BowlId==bowl.Id&&x.Action==ProductionAction.Design).Select(x=>x.Description).ToListAsync(ct);
        var existingTypeIds=existingDescriptions.Select(ParseDesignTypeId).Where(x=>x.HasValue).Select(x=>x!.Value).ToHashSet();
        if(typeIds.Any(existingTypeIds.Contains))return Conflict("یک یا چند دیزاین انتخاب‌شده قبلاً برای این کد ثبت شده است.");
        foreach(var entry in request.Items.GroupBy(x=>x.DesignTypeId).Select(x=>x.First()))
        { var type=types[entry.DesignTypeId];var userId=entry.UserId??currentUserId;var description=$"DESIGN:{type.Id}:{type.Name}:BOTTOM:{(request.BottomBowlDesigned?1:0)}";db.ProductionEvents.Add(new ProductionEvent(handpan?.Id,assembly?.Id,bowl.Id,userId,ProductionAction.Design,EventResult.Completed,null,description)); }
        await db.SaveChangesAsync(ct);return Ok(new{Count=typeIds.Count,request.BottomBowlDesigned});
    }

    private static Guid? ParseDesignTypeId(string? value)
    { if(string.IsNullOrWhiteSpace(value)||!value.StartsWith("DESIGN:"))return null;var parts=value.Split(':');return parts.Length>1&&Guid.TryParse(parts[1],out var id)?id:null; }
}
public sealed record DesignTypeRequest(string? Name, decimal Rate = 0);
public sealed record DesignRateRequest(decimal Rate);
public sealed record RegisterDesignItem(Guid DesignTypeId, Guid? UserId);
public sealed record RegisterDesignRequest(string? ProductionCode,List<RegisterDesignItem>? Items,bool BottomBowlDesigned);
