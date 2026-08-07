namespace TORSEPAN.Panel.Models;

public sealed class WarehouseHandpanDto
{
    public Guid HandpanId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string TopBowlCode { get; set; } = string.Empty;
    public string BottomBowlCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string ScaleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? WarehouseEntryDate { get; set; }
}
