Exit code: 0
Wall time: 0.6 seconds
Output:
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<int>()
            .HasDefaultValue(MaterialCategory.Other)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.TopBowlQuantity).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.BottomBowlQuantity).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LowStockThreshold).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.TopBowlLowStockThreshold).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.BottomBowlLowStockThreshold).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.TopBowlCodeTemplate).HasMaxLength(20).HasDefaultValue("").IsRequired();
        builder.Property(x => x.BottomBowlCodeTemplate).HasMaxLength(20).HasDefaultValue("").IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}

