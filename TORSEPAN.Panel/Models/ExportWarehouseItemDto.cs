namespace TORSEPAN.Panel.Models;

public sealed class ExportWarehouseItemDto
{
    public Guid Id { get; set; }
    public string ProductionCode { get; set; } = string.Empty;
    public int BowlType { get; set; }
    public string MaterialName { get; set; } = string.Empty;
}
