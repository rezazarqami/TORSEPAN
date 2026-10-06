namespace TORSEPAN.Domain.Entities;

public sealed class MessagePushKeys
{
    public int Id { get; set; } = 1;
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}
public sealed class MessagePushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int CredentialVersion { get; set; }
    public string Endpoint { get; set; } = "";
    public string EndpointHash { get; set; } = "";
    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class MessagePushDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public int CredentialVersion { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);
    public int Attempts { get; set; }
}
