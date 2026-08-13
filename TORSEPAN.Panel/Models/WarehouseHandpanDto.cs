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
    public List<WarehouseOperationDto> Operations { get; set; } = [];
    public List<string> PackagingItems { get; set; } = [];
}

public sealed class WarehouseOperationDto
{
    public string Operation { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}
