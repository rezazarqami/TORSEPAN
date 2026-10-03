using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Configurations;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> b)
    {
        b.ToTable("CustomerOrders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ScaleName).HasMaxLength(200).IsRequired();
        b.Property(x => x.InstrumentCode).HasMaxLength(50);
        b.HasIndex(x => x.CreatedAtUtc);
        b.HasIndex(x => x.DueAtUtc);
        b.HasIndex(x => x.TopBowlId).IsUnique();
        b.HasIndex(x => x.HandpanId).IsUnique();
        b.HasIndex(x => x.InstrumentCode).IsUnique();
        b.HasOne<Scale>().WithMany().HasForeignKey(x => x.ScaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CodeAssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Bowl>().WithMany().HasForeignKey(x => x.TopBowlId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Handpan>().WithMany().HasForeignKey(x => x.HandpanId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Reminders).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderReminderConfiguration : IEntityTypeConfiguration<OrderReminder>
{
    public void Configure(EntityTypeBuilder<OrderReminder> b)
    {
        b.ToTable("OrderReminders");
        b.HasKey(x => new { x.OrderId, x.Milestone });
        b.Ignore(x => x.DeliveryKey);
        b.Property(x => x.LastError).HasMaxLength(300);
        b.HasIndex(x => new { x.SentAtUtc, x.NextAttemptAtUtc });
    }
}
