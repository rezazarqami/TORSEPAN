Exit code: 0
Wall time: 0.6 seconds
Output:
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public class HandpanConfiguration : IEntityTypeConfiguration<Handpan>
{
    public void Configure(EntityTypeBuilder<Handpan> builder)
    {
        builder.ToTable("Handpans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SerialNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.SerialNumber)
            .IsUnique();

        builder.HasOne(x => x.Scale)
            .WithMany(x => x.Handpans)
            .HasForeignKey(x => x.ScaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Stage)
            .IsRequired();
        builder.Property(x => x.BuyerName).HasMaxLength(200);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.SaleDestination).HasMaxLength(200);

        // -----------------------------
        // Assembly (One-To-One)
        // -----------------------------

        builder.HasOne(x => x.Assembly)
            .WithOne(x => x.Handpan)
            .HasForeignKey<Handpan>(x => x.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Production Events
        // -----------------------------

        builder.HasMany(x => x.ProductionEvents)
            .WithOne(x => x.Handpan)
            .HasForeignKey(x => x.HandpanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

