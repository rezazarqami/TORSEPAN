namespace TORSEPAN.Panel.Models;

public sealed class HandpanDto
{
    public Guid Id { get; set; }

    public string TopBowlCode { get; set; } = string.Empty;

    public string BottomBowlCode { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public int Status { get; set; }

    public int Stage { get; set; }

    public DateTime CreatedAt { get; set; }
}
