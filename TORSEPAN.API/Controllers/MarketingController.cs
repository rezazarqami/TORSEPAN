using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController,Route("api/marketing"),Authorize]
public sealed class MarketingController(TORSEPANDbContext db):ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct)=>Ok(await db.MarketingLeads.AsNoTracking().Include(x=>x.Activities).OrderByDescending(x=>x.Priority).ThenBy(x=>x.Name).ToListAsync(ct));
    [HttpPost] public async Task<IActionResult> Save(MarketingLeadRequest r,CancellationToken ct)
    {
        MarketingLead lead;
        if(r.Id.HasValue){lead=await db.MarketingLeads.FirstAsync(x=>x.Id==r.Id,ct);}
        else{lead=new MarketingLead(r.Name,r.Country,r.City);db.MarketingLeads.Add(lead);}
        lead.Update(r.Name,r.CompanyName,r.Country,r.City,r.Website,r.Email,r.Phone,r.SocialContact,r.ContactPerson,r.Priority,r.Status,r.CooperationScore,r.SamplePurchaseScore,r.CurrentProducts,r.Target,r.Notes);
        await db.SaveChangesAsync(ct);return Ok(new{lead.Id});
    }
    [HttpPost("{id:guid}/activities")] public async Task<IActionResult> AddActivity(Guid id,MarketingActivityRequest r,CancellationToken ct)
    { if(!await db.MarketingLeads.AnyAsync(x=>x.Id==id,ct))return NotFound();db.MarketingActivities.Add(new MarketingActivity(id,r.Date,r.Method,r.SentText,r.Result,r.NextAction,User.Identity?.Name??"کاربر"));await db.SaveChangesAsync(ct);return NoContent(); }
}
public sealed record MarketingLeadRequest(Guid? Id,string Name,string CompanyName,string Country,string City,string Website,string Email,string Phone,string SocialContact,string ContactPerson,int Priority,string Status,decimal CooperationScore,decimal SamplePurchaseScore,string CurrentProducts,string Target,string Notes);
public sealed record MarketingActivityRequest(DateTime Date,string Method,string SentText,string Result,string NextAction);
