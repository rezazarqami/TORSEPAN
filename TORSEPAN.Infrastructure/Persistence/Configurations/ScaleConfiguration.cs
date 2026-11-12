using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class ScaleConfiguration : IEntityTypeConfiguration<Scale>, IEntityTypeConfiguration<ScalePhotos>
{
    public void Configure(EntityTypeBuilder<Scale> builder)
    {
        builder.ToTable("Scales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Usage).HasConversion<int>().IsRequired().HasDefaultValue(TORSEPAN.Domain.Enums.ScaleUsage.All);
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ScalePhotos> builder)
    {
        builder.ToTable("ScalePhotos");
        builder.HasKey(x => x.ScaleId);
        builder.HasOne<Scale>().WithOne().HasForeignKey<ScalePhotos>(x => x.ScaleId);
    }
}
