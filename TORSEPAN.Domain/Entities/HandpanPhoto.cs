using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class HandpanPhoto : Entity
{
    private HandpanPhoto() { }
    public HandpanPhoto(Guid handpanId, byte[] image, byte[] thumbnail, string contentType, Guid uploadedByUserId)
    {
        HandpanId = handpanId; Image = image; Thumbnail = thumbnail; ContentType = contentType;
        UploadedByUserId = uploadedByUserId; CreatedAt = DateTime.UtcNow;
    }
    public Guid HandpanId { get; private set; }
    public Handpan Handpan { get; private set; } = null!;
    public byte[] Image { get; private set; } = [];
    public byte[] Thumbnail { get; private set; } = [];
    public string ContentType { get; private set; } = "image/webp";
    public Guid UploadedByUserId { get; private set; }
    public User UploadedByUser { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
}
