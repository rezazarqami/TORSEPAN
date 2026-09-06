using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;
namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class WorkshopMessageConfiguration : IEntityTypeConfiguration<WorkshopMessage>
{
    public void Configure(EntityTypeBuilder<WorkshopMessage> b)
    {
        b.ToTable("WorkshopMessages"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.Title).HasMaxLength(150).IsRequired();
        b.Property(x=>x.Body).HasMaxLength(4000).IsRequired();
        b.HasOne(x=>x.Sender).WithMany().HasForeignKey(x=>x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>x.CreatedAt);
    }
}
public sealed class WorkshopMessageReceiptConfiguration : IEntityTypeConfiguration<WorkshopMessageReceipt>
{
    public void Configure(EntityTypeBuilder<WorkshopMessageReceipt> b)
    {
        b.ToTable("WorkshopMessageReceipts"); b.HasKey(x=>new{x.MessageId,x.RecipientId});
        b.HasOne(x=>x.Message).WithMany().HasForeignKey(x=>x.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x=>x.Recipient).WithMany().HasForeignKey(x=>x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.RecipientId,x.ReadAt});
    }
}

