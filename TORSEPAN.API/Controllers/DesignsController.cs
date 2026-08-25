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
    public async Task<IActionResult> Types(CancellationToken ct) => Ok(await db.DesignTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));

    [HttpPost("types"), Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> AddType(DesignTypeRequest request, CancellationToken ct)
    {
        var name=request.Name?.Trim()??""; if(name.Length==0)return BadRequest("نام دیزاین الزامی است.");
        if(await db.DesignTypes.AnyAsync(x=>x.Name.ToLower()==name.ToLower()&&x.IsActive,ct))return Conflict("این نوع دیزاین قبلاً ثبت شده است.");
        var item=new DesignType(name);db.DesignTypes.Add(item);await db.SaveChangesAsync(ct);return Ok(new{id=item.Id,item.Name});
    }

    [HttpDelete("types/{id:guid}"), Authorize(Roles = "Administrator,ProductionManager")]
    public async Task<IActionResult> DeleteType(Guid id,CancellationToken ct){var item=await db.DesignTypes.FirstOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return NotFound();item.Deactivate();await db.SaveChangesAsync(ct);return NoContent();}

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDesignRequest request,CancellationToken ct)
    {
        var code=ProductionCodeNormalizer.Normalize(request.ProductionCode??"");
        var bowl=await db.Bowls.FirstOrDefaultAsync(x=>x.ProductionCode==code,ct);if(bowl is null)return NotFound("کاسه یا ساز پیدا نشد.");
        var type=await db.DesignTypes.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==request.DesignTypeId&&x.IsActive,ct);if(type is null)return BadRequest("نوع دیزاین معتبر نیست.");
        if(await db.ProductionEvents.AnyAsync(x=>x.BowlId==bowl.Id&&x.Action==ProductionAction.Design,ct))return Conflict("برای این کد قبلاً دیزاین ثبت شده است.");
        var assembly=await db.HandpanAssemblies.FirstOrDefaultAsync(x=>x.TopBowlId==bowl.Id||x.BottomBowlId==bowl.Id,ct);
        var handpan=assembly is null?null:await db.Handpans.FirstOrDefaultAsync(x=>x.AssemblyId==assembly.Id,ct);
        var userId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??throw new UnauthorizedAccessException());
        var description=$"DESIGN:{type.Id}:{type.Name}:BOTTOM:{(request.BottomBowlDesigned?1:0)}";
        db.ProductionEvents.Add(new ProductionEvent(handpan?.Id,assembly?.Id,bowl.Id,userId,ProductionAction.Design,EventResult.Completed,null,description));
        await db.SaveChangesAsync(ct);return Ok(new{type.Name,request.BottomBowlDesigned});
    }
}
public sealed record DesignTypeRequest(string? Name);
public sealed record RegisterDesignRequest(string? ProductionCode,Guid DesignTypeId,bool BottomBowlDesigned);
