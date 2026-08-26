using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class DesignTypeConfiguration : IEntityTypeConfiguration<DesignType>
{
    public void Configure(EntityTypeBuilder<DesignType> builder)
    {
        builder.ToTable("DesignTypes"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired(); builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Rate).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired(); builder.Property(x => x.IsActive).IsRequired();
    }
}
