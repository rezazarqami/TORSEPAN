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

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Stage)
            .IsRequired();

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