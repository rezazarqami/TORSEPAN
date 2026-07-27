namespace TORSEPAN.Application.ProductionEvents.Queries.GetFinishedHandpans;

public sealed class GetFinishedHandpansResponse
{
    public Guid Id { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}