namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionQueue;

public sealed class GetProductionQueueResponse
{
    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}