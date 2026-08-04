namespace TORSEPAN.Panel.Models;

public sealed class HandpanDto
{
    public Guid Id { get; set; }

    public Guid TopBowlId { get; set; }

    public Guid BottomBowlId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public int Status { get; set; }

    public int Stage { get; set; }

    public DateTime CreatedAt { get; set; }
}