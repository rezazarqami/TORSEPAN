using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public class BowlConfiguration : IEntityTypeConfiguration<Bowl>
{
    public void Configure(EntityTypeBuilder<Bowl> builder)
    {
        builder.ToTable("Bowls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductionCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.ProductionCode)
            .IsUnique();

        builder.Property(x => x.BowlType)
            .IsRequired();

        builder.Property(x => x.HasNotes)
            .IsRequired();

        builder.Property(x => x.InstrumentType)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Stage)
            .IsRequired();

        builder.HasOne(x => x.Scale)
            .WithMany()
            .HasForeignKey(x => x.ScaleId)
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------
        // Production Events
        // -----------------------------

        builder.HasMany(x => x.ProductionEvents)
            .WithOne(x => x.Bowl)
            .HasForeignKey(x => x.BowlId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Assembly (Top Bowl)
        // -----------------------------

        builder.HasMany(x => x.TopAssemblies)
            .WithOne(x => x.TopBowl)
            .HasForeignKey(x => x.TopBowlId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // Assembly (Bottom Bowl)
        // -----------------------------

        builder.HasMany(x => x.BottomAssemblies)
            .WithOne(x => x.BottomBowl)
            .HasForeignKey(x => x.BottomBowlId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
