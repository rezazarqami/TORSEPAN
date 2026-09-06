using TORSEPAN.Domain.Common;
namespace TORSEPAN.Domain.Entities;

public sealed class WorkshopMessage : Entity
{
    private WorkshopMessage() { }
    public WorkshopMessage(Guid id, Guid senderId, string title, string body, bool broadcast)
    { Id=id; SenderId=senderId; Title=title; Body=body; Broadcast=broadcast; CreatedAt=DateTime.UtcNow; }
    public Guid SenderId { get; private set; }
    public User Sender { get; private set; } = null!;
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public bool Broadcast { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
public sealed class WorkshopMessageReceipt
{
    private WorkshopMessageReceipt() { }
    public WorkshopMessageReceipt(Guid messageId, Guid recipientId) { MessageId=messageId; RecipientId=recipientId; }
    public Guid MessageId { get; private set; }
    public WorkshopMessage Message { get; private set; } = null!;
    public Guid RecipientId { get; private set; }
    public User Recipient { get; private set; } = null!;
    public DateTime? ReadAt { get; private set; }
}

