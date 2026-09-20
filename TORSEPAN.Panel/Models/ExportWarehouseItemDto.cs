namespace TORSEPAN.Panel.Models;

public sealed class ExportWarehouseItemDto
{
    public Guid Id { get; set; }
    public string ProductionCode { get; set; } = string.Empty;
    public string ItemType { get; set; } = "bowl";
    public string ItemKind { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public int Location { get; set; }
}
