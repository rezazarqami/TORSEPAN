using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;
public sealed class PayrollRateConfiguration:IEntityTypeConfiguration<PayrollRate>
{
 public void Configure(EntityTypeBuilder<PayrollRate> b){b.ToTable("PayrollRates");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();b.Property(x=>x.Action).HasConversion<int>();b.Property(x=>x.BowlType).HasConversion<int?>();b.Property(x=>x.IsExport).HasDefaultValue(false);b.Property(x=>x.Amount).HasPrecision(18,2);b.HasOne(x=>x.Material).WithMany().HasForeignKey(x=>x.MaterialId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.Scale).WithMany().HasForeignKey(x=>x.ScaleId).OnDelete(DeleteBehavior.Cascade);}
}
