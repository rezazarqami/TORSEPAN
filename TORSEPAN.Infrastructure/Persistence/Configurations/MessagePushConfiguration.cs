using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;
namespace TORSEPAN.Infrastructure.Persistence.Configurations;
public sealed class MessagePushKeysConfiguration : IEntityTypeConfiguration<MessagePushKeys>
{
    public void Configure(EntityTypeBuilder<MessagePushKeys> b)
    {
        b.ToTable("MessagePushKeys"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PublicKey).HasMaxLength(100); b.Property(x => x.PrivateKey).HasMaxLength(100);
    }
}
public sealed class MessagePushSubscriptionConfiguration : IEntityTypeConfiguration<MessagePushSubscription>
{
    public void Configure(EntityTypeBuilder<MessagePushSubscription> b)
    {
        b.ToTable("MessagePushSubscriptions"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Endpoint).HasMaxLength(2048); b.Property(x => x.EndpointHash).HasMaxLength(64);
        b.Property(x => x.P256dh).HasMaxLength(100); b.Property(x => x.Auth).HasMaxLength(50);
        b.HasIndex(x => x.EndpointHash).IsUnique(); b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class MessagePushDeliveryConfiguration : IEntityTypeConfiguration<MessagePushDelivery>
{
    public void Configure(EntityTypeBuilder<MessagePushDelivery> b)
    {
        b.ToTable("MessagePushDeliveries"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.MessageId, x.SubscriptionId }).IsUnique(); b.HasIndex(x => x.NextAttemptAt);
        b.HasOne<MessagePushSubscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<WorkshopMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
