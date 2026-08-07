using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public class HandpanAssemblyConfiguration : IEntityTypeConfiguration<HandpanAssembly>
{
    public void Configure(EntityTypeBuilder<HandpanAssembly> builder)
    {
        builder.ToTable("HandpanAssemblies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AssemblyDate)
            .IsRequired();

        builder.HasIndex(x => x.TopBowlId)
            .IsUnique();

        builder.HasIndex(x => x.BottomBowlId)
            .IsUnique();

        // -----------------------------
        // Top Bowl
        // -----------------------------

        builder.HasOne(x => x.TopBowl)
            .WithMany(x => x.TopAssemblies)
            .HasForeignKey(x => x.TopBowlId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Bottom Bowl
        // -----------------------------

        builder.HasOne(x => x.BottomBowl)
            .WithMany(x => x.BottomAssemblies)
            .HasForeignKey(x => x.BottomBowlId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Handpan
        // -----------------------------

        builder.HasOne(x => x.Handpan)
            .WithOne(x => x.Assembly)
            .HasForeignKey<Handpan>(x => x.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Production Events
        // -----------------------------

        builder.HasMany(x => x.ProductionEvents)
            .WithOne(x => x.Assembly)
            .HasForeignKey(x => x.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
