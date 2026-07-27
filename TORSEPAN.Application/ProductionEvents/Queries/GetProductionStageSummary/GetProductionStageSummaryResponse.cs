namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStageSummary;

public sealed class GetProductionStageSummaryResponse
{
    public string Stage { get; set; } = string.Empty;

    public int Count { get; set; }
}