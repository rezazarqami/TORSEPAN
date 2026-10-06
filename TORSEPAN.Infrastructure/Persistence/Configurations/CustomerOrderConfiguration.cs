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
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
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

public sealed class CustomerOrderLineConfiguration : IEntityTypeConfiguration<CustomerOrderLine>
{
    public void Configure(EntityTypeBuilder<CustomerOrderLine> b)
    {
        b.ToTable("CustomerOrderLines", t => t.HasCheckConstraint("CK_OrderLine_Quantity", "\"Quantity\" >= 1 AND \"Quantity\" <= 10000"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ScaleName).HasMaxLength(200).IsRequired();
        b.Property(x => x.DesignName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.OrderId, x.Position }).IsUnique();
        b.HasOne<Scale>().WithMany().HasForeignKey(x => x.ScaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DesignType>().WithMany().HasForeignKey(x => x.DesignTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Instruments).WithOne().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class OrderInstrumentConfiguration : IEntityTypeConfiguration<OrderInstrument>
{
    public void Configure(EntityTypeBuilder<OrderInstrument> b)
    {
        b.ToTable("OrderInstruments", t => t.HasCheckConstraint("CK_OrderInstrument_Slot", "\"Slot\" >= 1"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.HasIndex(x => new { x.LineId, x.Slot }).IsUnique();
        b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => x.TopBowlId).IsUnique(); b.HasIndex(x => x.HandpanId).IsUnique();
        b.HasOne<Bowl>().WithMany().HasForeignKey(x => x.TopBowlId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Handpan>().WithMany().HasForeignKey(x => x.HandpanId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
