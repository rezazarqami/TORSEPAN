using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;
public sealed class HandpanPhotoConfiguration : IEntityTypeConfiguration<HandpanPhoto>
{
    public void Configure(EntityTypeBuilder<HandpanPhoto> b)
    {
        b.ToTable("HandpanPhotos"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Image).IsRequired(); b.Property(x => x.Thumbnail).IsRequired();
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(50);
        b.HasIndex(x => new { x.HandpanId, x.CreatedAt });
        b.HasOne(x => x.Handpan).WithMany().HasForeignKey(x => x.HandpanId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
