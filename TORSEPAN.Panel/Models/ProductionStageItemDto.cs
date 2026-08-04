namespace TORSEPAN.Panel.Models;

public sealed class ProductionStageItemDto
{
    public Guid Id { get; set; }

    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public int Stage { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}