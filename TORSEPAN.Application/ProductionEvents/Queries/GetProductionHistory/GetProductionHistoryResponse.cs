namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionHistory;

public sealed class GetProductionHistoryResponse
{
    public Guid EventId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string? Description { get; set; }
}