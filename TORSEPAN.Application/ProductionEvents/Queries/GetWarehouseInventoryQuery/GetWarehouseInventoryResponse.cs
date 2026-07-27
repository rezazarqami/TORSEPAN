namespace TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;

public sealed class GetWarehouseInventoryResponse
{
    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}