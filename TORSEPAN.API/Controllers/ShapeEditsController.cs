using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/shape-edits"), Authorize(Roles="Administrator")]
public sealed class ShapeEditsController(TORSEPANDbContext db):ControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code,CancellationToken ct)
    {
        var ids=await ResolveAsync(code,ct);
        if(ids.Count==0)return NotFound(new{Message="ساز یا کاسه پیدا نشد."});
        var events=await db.ProductionEvents.AsNoTracking().Include(x=>x.User).Include(x=>x.Bowl)
            .Where(x=>ids.Contains(x.BowlId??Guid.Empty)&&x.Action==ProductionAction.Shape&&x.Result==EventResult.Completed&&!x.Description.StartsWith("NOTE:"))
            .OrderBy(x=>x.EventDate).ToListAsync(ct);
        var userIds=events.SelectMany(x=>x.ShapeContributors().Values).Distinct().ToArray();
        var names=await db.Users.Where(x=>userIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>string.IsNullOrWhiteSpace(x.FullName)?x.UserName:x.FullName,ct);
        return Ok(events.Select(x=>new{EventId=x.Id,BowlCode=x.Bowl?.ProductionCode,PerformedBy=string.IsNullOrWhiteSpace(x.User.FullName)?x.User.UserName:x.User.FullName,x.EventDate,ExpectedDescription=x.Description,
            Contributors=x.ShapeContributors().Select(c=>new{Key=c.Key,UserId=c.Value,Name=names.GetValueOrDefault(c.Value,"کاربر ثبت‌شده")})}));
    }
    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code,ShapeEditRequest request,CancellationToken ct)
    {
        if(!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor))return Unauthorized();
        var additions=new Dictionary<string,Guid>();
        if(request.StretchUserId.HasValue)additions["Stretch"]=request.StretchUserId.Value;
        if(request.NoteAreaUserId.HasValue)additions["NoteArea"]=request.NoteAreaUserId.Value;
        if(request.EditUserId.HasValue)additions["Edit"]=request.EditUserId.Value;
        if(additions.Count==0||additions.Values.Any(x=>x==Guid.Empty))return BadRequest(new{Message="حداقل یک انجام‌دهنده را انتخاب کنید."});
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var ids=await ResolveAsync(code,ct);
        var ev=await db.ProductionEvents.SingleOrDefaultAsync(x=>x.Id==request.EventId&&ids.Contains(x.BowlId??Guid.Empty)&&x.Action==ProductionAction.Shape&&x.Result==EventResult.Completed&&!x.Description.StartsWith("NOTE:"),ct);
        if(ev is null)return NotFound(new{Message="عملیات شیپ متعلق به این کد پیدا نشد."});
        if(ev.Description!=request.ExpectedDescription)return Conflict(new{Message="اطلاعات شیپ تغییر کرده؛ دوباره جست‌وجو کنید."});
        var valid=await db.Users.Where(x=>additions.Values.Contains(x.Id)&&x.IsActive&&x.UserRoles.Any(r=>r.Role.Name=="Shaper")).Select(x=>x.Id).ToListAsync(ct);
        if(additions.Values.Any(x=>!valid.Contains(x)))return BadRequest(new{Message="انجام‌دهنده باید شیپر فعال باشد."});
        try{ev.AddShapeContributors(additions);}catch(InvalidOperationException e){return Conflict(new{Message=e.Message});}
        var audit=new ProductionEvent(ev.HandpanId,ev.AssemblyId,ev.BowlId,actor,ProductionAction.Created,EventResult.Completed,null,$"NOTE:SHAPE-EDIT:{ev.Id}:{request.ExpectedDescription} => {ev.Description}");
        audit.SetPayrollExcluded(true,actor);db.ProductionEvents.Add(audit);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Ok(new{Saved=true});
    }
    private async Task<List<Guid>> ResolveAsync(string code,CancellationToken ct)
    {
        var normalized=ProductionCodeNormalizer.Normalize(code);
        var bowl=await db.Bowls.Where(x=>x.ProductionCode==normalized).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        if(bowl.HasValue)return [bowl.Value];
        var pair=await db.Handpans.Where(x=>x.SerialNumber==normalized).Select(x=>new{x.Assembly.TopBowlId,x.Assembly.BottomBowlId}).SingleOrDefaultAsync(ct);
        return pair is null?[]:[pair.TopBowlId,pair.BottomBowlId];
    }
}
public sealed record ShapeEditRequest(Guid EventId,string ExpectedDescription,Guid? StretchUserId,Guid? NoteAreaUserId,Guid? EditUserId);
