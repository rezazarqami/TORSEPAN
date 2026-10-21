using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.API.Services;
public sealed class SaleProfitService(TORSEPANDbContext db)
{
 public async Task UpdateAsync(Handpan handpan,decimal? profit,MoneyCurrency currency,bool preserveRecipients=false)
 {
  var old=await db.SaleProfitEntries.Where(x=>x.HandpanId==handpan.Id&&x.ReversedAt==null).ToListAsync();
  var original=old;
  if(preserveRecipients&&original.Count==0){var prior=await db.SaleProfitEntries.Where(x=>x.HandpanId==handpan.Id&&x.SoldAt==handpan.SoldAt).OrderByDescending(x=>x.CreatedAt).ToListAsync();if(prior.Count>0){var latest=prior.First().AllocationId;original=prior.Where(x=>x.AllocationId==latest).ToList();}}
  // A repeated edit with the same values must not create duplicate credits.
  if(preserveRecipients&&handpan.NetProfit==profit&&handpan.ProfitCurrency==currency)return;
  foreach(var row in old)row.Reverse();
  if(!profit.HasValue||profit==0)return;
  var roles=new List<(ProductionAction Action,decimal Percent,List<(Guid Id,string Name)> People)>();
  if(preserveRecipients&&original.Count>0){foreach(var group in original.GroupBy(x=>x.Action))roles.Add((group.Key,group.First().RolePercent,group.Select(x=>(x.UserId,x.UserName)).Distinct().ToList()));}
  else {
   var pair=await db.HandpanAssemblies.AsNoTracking().SingleAsync(x=>x.Id==handpan.AssemblyId);
   var events=await db.ProductionEvents.AsNoTracking().Include(x=>x.User).Where(x=>x.Result==EventResult.Completed&&
    (x.HandpanId==handpan.Id||x.AssemblyId==handpan.AssemblyId||x.BowlId==pair.TopBowlId||x.BowlId==pair.BottomBowlId)&&!x.Description.StartsWith("NOTE:")).ToListAsync();
   foreach(var rule in await db.SaleProfitRules.AsNoTracking().Where(x=>x.Percent>0).ToListAsync())
    roles.Add((rule.Action,rule.Percent,events.Where(x=>x.Action==rule.Action).Select(x=>(x.UserId,string.IsNullOrWhiteSpace(x.User.FullName)?x.User.UserName:x.User.FullName)).DistinctBy(x=>x.UserId).ToList()));
  }
  var factor=currency==MoneyCurrency.Usd?100m:1m;
  var eligible=roles.Where(x=>x.People.Count>0).OrderBy(x=>x.Action).ToList();
  var units=eligible.Select(x=>profit.Value*x.Percent*factor/100).ToArray();
  var allocated=units.Select(decimal.Floor).ToArray();
  var target=decimal.Round(units.Sum(),0,MidpointRounding.AwayFromZero);
  foreach(var index in Enumerable.Range(0,units.Length).OrderByDescending(i=>units[i]-allocated[i]).ThenBy(i=>eligible[i].Action).Take((int)(target-allocated.Sum())))allocated[index]++;
  var allocationId=Guid.NewGuid();
  for(int r=0;r<eligible.Count;r++){
   var role=eligible[r];var people=role.People.OrderBy(x=>x.Id).ToList();var total=allocated[r]/factor;
   var each=Math.Floor(total*factor/people.Count)/factor;
   for(int i=0;i<people.Count;i++)db.SaleProfitEntries.Add(new(handpan.Id,handpan.SerialNumber,people[i].Id,people[i].Name,role.Action,role.Percent,profit.Value,i==people.Count-1?total-each*(people.Count-1):each,currency,handpan.SoldAt??DateTime.UtcNow,allocationId));
  }
 }
}
