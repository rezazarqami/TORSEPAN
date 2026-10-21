using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Controllers;
[ApiController,Route("api/profit-sharing"),Authorize(Roles="Administrator,ProductionManager")]
public sealed class ProfitSharingController(TORSEPANDbContext db):ControllerBase
{
 [HttpGet("rules")]public async Task<IActionResult> Rules()=>Ok(await db.SaleProfitRules.AsNoTracking().OrderBy(x=>x.Action).ToListAsync());
 [HttpPut("rules")]public async Task<IActionResult> SaveRules(List<ProfitRuleUpdate> changes){
  var rules=await db.SaleProfitRules.ToListAsync();
  if(changes.Count!=6||changes.Select(x=>x.Action).Distinct().Count()!=6||changes.Any(x=>!rules.Any(r=>(int)r.Action==x.Action)||x.Percent<0||x.Percent>100||decimal.Round(x.Percent,2)!=x.Percent)||changes.Sum(x=>x.Percent)>100)
   return BadRequest(new{Message="درصد شش مرحله را بین صفر تا صد وارد کنید؛ جمع آن‌ها نباید بیش از صد باشد."});
  if(changes.Any(x=>rules.Single(r=>(int)r.Action==x.Action).Version!=x.Version))return Conflict(new{Message="درصدها هم‌زمان تغییر کرده‌اند؛ صفحه را تازه کنید."});
  foreach(var change in changes)rules.Single(x=>(int)x.Action==change.Action).Set(change.Percent);
  try{await db.SaveChangesAsync();return Ok(rules.OrderBy(x=>x.Action));}catch(DbUpdateConcurrencyException){return Conflict(new{Message="درصدها هم‌زمان تغییر کرده‌اند؛ دوباره دریافت کنید."});}
 }
 [HttpGet("members")]public async Task<IActionResult> Members(){var rows=await db.SaleProfitEntries.AsNoTracking().ToListAsync();return Ok(rows.GroupBy(x=>x.UserId).Select(g=>new{UserId=g.Key,Name=g.OrderByDescending(x=>x.CreatedAt).First().UserName,Toman=g.Where(x=>x.ReversedAt==null&&x.Currency==MoneyCurrency.Toman).Sum(x=>x.Amount),Usd=g.Where(x=>x.ReversedAt==null&&x.Currency==MoneyCurrency.Usd).Sum(x=>x.Amount),Instruments=g.Where(x=>x.ReversedAt==null).Select(x=>x.HandpanId).Distinct().Count()}).OrderBy(x=>x.Name));}
 [HttpGet("members/{id:guid}")]public async Task<IActionResult> Member(Guid id)=>Ok(await db.SaleProfitEntries.AsNoTracking().Where(x=>x.UserId==id).OrderByDescending(x=>x.CreatedAt).ToListAsync());
}
public sealed record ProfitRuleUpdate(int Action,decimal Percent,Guid Version);
