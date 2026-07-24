using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public class ProductionEventConfiguration : IEntityTypeConfiguration<ProductionEvent>
{
    public void Configure(EntityTypeBuilder<ProductionEvent> builder)
    {
        builder.ToTable("ProductionEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.Result)
            .IsRequired();

        builder.Property(x => x.EventDate)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bowl)
            .WithMany(x => x.ProductionEvents)
            .HasForeignKey(x => x.BowlId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Assembly)
            .WithMany(x => x.ProductionEvents)
            .HasForeignKey(x => x.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Handpan)
            .WithMany(x => x.ProductionEvents)
            .HasForeignKey(x => x.HandpanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}