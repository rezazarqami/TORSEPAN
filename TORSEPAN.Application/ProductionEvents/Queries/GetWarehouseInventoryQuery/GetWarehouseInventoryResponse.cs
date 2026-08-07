namespace TORSEPAN.Application.ProductionEvents.Queries.GetWarehouseInventory;

public sealed class GetWarehouseInventoryResponse
{
    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string TopBowlCode { get; set; } = string.Empty;

    public string BottomBowlCode { get; set; } = string.Empty;

    public string MaterialName { get; set; } = string.Empty;

    public string ScaleName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? WarehouseEntryDate { get; set; }

    public IReadOnlyList<WarehouseOperationResponse> Operations { get; set; }
        = [];
}

public sealed class WarehouseOperationResponse
{
    public string Operation { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; }
}
