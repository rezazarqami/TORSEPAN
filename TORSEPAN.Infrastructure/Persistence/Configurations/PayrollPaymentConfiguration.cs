using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class PayrollPaymentConfiguration : IEntityTypeConfiguration<PayrollPayment>
{
    public void Configure(EntityTypeBuilder<PayrollPayment> builder)
    {
        builder.ToTable("PayrollPayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaidBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.HandpanIdsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.HandpanCodesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.LinesJson).HasColumnType("text").IsRequired();
        builder.HasIndex(x => x.PaidAt);
    }
}
