using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
namespace TORSEPAN.Infrastructure.Persistence.Configurations;
public sealed class SaleProfitRuleConfiguration:IEntityTypeConfiguration<SaleProfitRule>
{
 public void Configure(EntityTypeBuilder<SaleProfitRule> b){b.ToTable("SaleProfitRules");b.HasKey(x=>x.Action);b.Property(x=>x.Action).ValueGeneratedNever();b.Property(x=>x.Percent).HasPrecision(5,2);b.Property(x=>x.Version).IsConcurrencyToken();
 b.HasData(new[]{(2,2m),(3,7m),(5,1m),(6,13m),(7,13m),(13,0m)}.Select(x=>new{Action=(ProductionAction)x.Item1,Percent=x.Item2,Version=Guid.Parse($"00000000-0000-4000-8000-{x.Item1:D12}")}));}
}
public sealed class SaleProfitEntryConfiguration:IEntityTypeConfiguration<SaleProfitEntry>
{
 public void Configure(EntityTypeBuilder<SaleProfitEntry> b){b.ToTable("SaleProfitEntries");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();b.Property(x=>x.Code).HasMaxLength(50).IsRequired();b.Property(x=>x.UserName).HasMaxLength(300).IsRequired();b.Property(x=>x.RolePercent).HasPrecision(5,2);b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.NetProfit).HasPrecision(18,2);b.HasIndex(x=>new{x.HandpanId,x.ReversedAt});b.HasIndex(x=>new{x.UserId,x.Currency});}
}
public sealed class AccountingCurrencyConfiguration:IEntityTypeConfiguration<AccountingDocument>
{public void Configure(EntityTypeBuilder<AccountingDocument> b){b.Property(x=>x.Currency).HasDefaultValue(MoneyCurrency.Toman);}}
