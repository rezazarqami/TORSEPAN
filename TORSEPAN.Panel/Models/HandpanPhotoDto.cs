namespace TORSEPAN.Panel.Models;
public sealed class HandpanPhotoDto { public Guid Id { get; set; } public DateTime CreatedAt { get; set; } }
public sealed class WarehouseGalleryPhotoDto { public Guid HandpanId { get; set; } public Guid Id { get; set; } public byte[] Thumbnail { get; set; } = []; }
