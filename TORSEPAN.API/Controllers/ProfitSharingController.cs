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
  if(changes.Count!=7||changes.Select(x=>x.Action).Distinct().Count()!=7||changes.Any(x=>!rules.Any(r=>(int)r.Action==x.Action)||x.Percent<0||x.Percent>100||decimal.Round(x.Percent,2)!=x.Percent)||changes.Sum(x=>x.Percent)>100)
   return BadRequest(new{Message="درصد هفت بخش را بین صفر تا صد وارد کنید؛ جمع آن‌ها نباید بیش از صد باشد."});
  if(changes.Any(x=>rules.Single(r=>(int)r.Action==x.Action).Version!=x.Version))return Conflict(new{Message="درصدها هم‌زمان تغییر کرده‌اند؛ صفحه را تازه کنید."});
  foreach(var change in changes)rules.Single(x=>(int)x.Action==change.Action).Set(change.Percent);
  try{await db.SaveChangesAsync();return Ok(rules.OrderBy(x=>x.Action));}catch(DbUpdateConcurrencyException){return Conflict(new{Message="درصدها هم‌زمان تغییر کرده‌اند؛ دوباره دریافت کنید."});}
 }
 [HttpGet("members")]
 public async Task<IActionResult> Members()
 {
  var rows=await db.SaleProfitEntries.AsNoTracking().ToListAsync();
  var users=await db.Users.AsNoTracking().Where(x=>!x.IsDeleted).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.FullName)
   .Select(x=>new{x.Id,Name=x.FullName==""?x.UserName:x.FullName}).ToListAsync();
  var byUser=rows.ToLookup(x=>x.UserId);
  var accounts=users.Select(x=>Summary(x.Id,x.Name,byUser[x.Id])).ToList();
  // Keep historical balances visible even if the original login was deleted.
  var known=users.Select(x=>x.Id).Append(HallProfitAccount.Id).ToHashSet();
  accounts.AddRange(rows.Where(x=>!known.Contains(x.UserId)).GroupBy(x=>x.UserId)
   .Select(g=>Summary(g.Key,g.OrderByDescending(x=>x.CreatedAt).First().UserName,g)));
  var hall=Summary(HallProfitAccount.Id,HallProfitAccount.Name,byUser[HallProfitAccount.Id]);
  var payments=await db.HallProfitPayments.AsNoTracking().ToListAsync();
  accounts.Add(hall with {PaidToman=payments.Where(x=>x.Currency==MoneyCurrency.Toman).Sum(x=>x.Amount),PaidUsd=payments.Where(x=>x.Currency==MoneyCurrency.Usd).Sum(x=>x.Amount)});
  return Ok(accounts);
 }
 [HttpGet("hall/payments")]
 public async Task<IActionResult> HallPayments()=>Ok(await db.HallProfitPayments.AsNoTracking().OrderByDescending(x=>x.PaidAt).ThenBy(x=>x.Id).ToListAsync());
 [HttpPost("hall/payments")]
 public async Task<IActionResult> PayHall(HallPaymentRequest request)
 {
  if(request.Id==Guid.Empty||!SaleMoney.Valid(request.Amount,request.Currency)||request.Amount<=0)return BadRequest(new{Message="مبلغ پرداخت را مثبت و با واحد معتبر وارد کنید."});
  var existing=await db.HallProfitPayments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==request.Id);
  if(existing is not null)return existing.Amount==request.Amount&&existing.Currency==request.Currency?Ok(existing):Conflict(new{Message="شناسه پرداخت قبلاً استفاده شده است."});
  var payment=new HallProfitPayment(request.Id,request.Amount,request.Currency,User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??"");
  db.HallProfitPayments.Add(payment);
  try{await db.SaveChangesAsync();}catch(DbUpdateException){db.Entry(payment).State=EntityState.Detached;existing=await db.HallProfitPayments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==request.Id);if(existing is null)throw;return existing.Amount==request.Amount&&existing.Currency==request.Currency?Ok(existing):Conflict(new{Message="شناسه پرداخت قبلاً استفاده شده است."});}
  return Ok(payment);
 }
 [HttpGet("members/{id:guid}")]
 public async Task<IActionResult> Member(Guid id)=>Ok(await Entries(id));
 private Task<List<SaleProfitEntry>> Entries(Guid id)=>db.SaleProfitEntries.AsNoTracking()
  .Where(x=>x.UserId==id).OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).ToListAsync();
 private static ProfitMemberAccount Summary(Guid id,string name,IEnumerable<SaleProfitEntry> entries)
 {
  var active=entries.Where(x=>x.ReversedAt==null).ToList();
  return new(id,name,active.Where(x=>x.Currency==MoneyCurrency.Toman).Sum(x=>x.Amount),
   active.Where(x=>x.Currency==MoneyCurrency.Usd).Sum(x=>x.Amount),active.Select(x=>x.HandpanId).Distinct().Count());
 }

}
public sealed record ProfitRuleUpdate(int Action,decimal Percent,Guid Version);

public sealed record ProfitMemberAccount(Guid UserId,string Name,decimal Toman,decimal Usd,int Instruments)
{public decimal PaidToman{get;init;}public decimal PaidUsd{get;init;}public decimal BalanceToman=>Toman-PaidToman;public decimal BalanceUsd=>Usd-PaidUsd;}
public sealed record HallPaymentRequest(Guid Id,decimal Amount,MoneyCurrency Currency);
