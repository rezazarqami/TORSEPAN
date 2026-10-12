using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/sales-attribution"), Authorize(Roles="Administrator,ProductionManager,SalesAdmin")]
public sealed class SalesAttributionController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) => Ok(new
    {
        Sources = await db.SaleLeadSources.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.RequiresReferrer}).ToListAsync(ct),
        Referrers = await db.SalesReferrers.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name}).ToListAsync(ct)
    });

    [HttpPost("sources")]
    public async Task<IActionResult> AddSource([FromBody]NameRequest request,CancellationToken ct)
    {
        var name=request.Name?.Trim();if(string.IsNullOrWhiteSpace(name))return BadRequest("عنوان روش آشنایی را وارد کنید.");
        if(await db.SaleLeadSources.AnyAsync(x=>x.Name==name,ct))return BadRequest("این روش قبلاً ثبت شده است.");
        var item=new SaleLeadSource(name);db.SaleLeadSources.Add(item);await db.SaveChangesAsync(ct);return Ok(new{item.Id,item.Name,item.RequiresReferrer});
    }

    [HttpPost("referrers")]
    public async Task<IActionResult> AddReferrer([FromBody]NameRequest request,CancellationToken ct)
    {
        var name=request.Name?.Trim();if(string.IsNullOrWhiteSpace(name))return BadRequest("نام معرف را وارد کنید.");
        if(await db.SalesReferrers.AnyAsync(x=>x.Name==name,ct))return BadRequest("این معرف قبلاً ثبت شده است.");
        var item=new SalesReferrer(name);db.SalesReferrers.Add(item);await db.SaveChangesAsync(ct);return Ok(new{item.Id,item.Name});
    }

    [HttpGet("report")]
    [Authorize(Roles="Administrator,ProductionManager")]
    public async Task<IActionResult> Report([FromQuery]DateTime? from,[FromQuery]DateTime? to,CancellationToken ct)
    {
        var start=DateTime.SpecifyKind((from??DateTime.UtcNow.AddMonths(-6)).Date,DateTimeKind.Utc);
        var end=DateTime.SpecifyKind((to??DateTime.UtcNow).Date.AddDays(1),DateTimeKind.Utc);
        var sales=await db.Handpans.AsNoTracking().Where(x=>x.SoldAt>=start&&x.SoldAt<end).ToListAsync(ct);
        var sources=await db.SaleLeadSources.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
        var referrers=await db.SalesReferrers.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
        return Ok(new{From=start,To=end.AddTicks(-1),Total=sales.Count,
            Sources=sales.GroupBy(x=>x.SaleLeadSourceId).Select(g=>new{Name=g.Key.HasValue?sources.GetValueOrDefault(g.Key.Value,"حذف‌شده"):"ثبت‌نشده",Count=g.Count(),Percentage=sales.Count==0?0:Math.Round(g.Count()*100d/sales.Count,1)}).OrderByDescending(x=>x.Count),
            Referrers=sales.Where(x=>x.SalesReferrerId.HasValue).GroupBy(x=>x.SalesReferrerId!.Value).Select(g=>new{Name=referrers.GetValueOrDefault(g.Key,"حذف‌شده"),Count=g.Count()}).OrderByDescending(x=>x.Count)});
    }
}
public sealed record NameRequest(string? Name);
