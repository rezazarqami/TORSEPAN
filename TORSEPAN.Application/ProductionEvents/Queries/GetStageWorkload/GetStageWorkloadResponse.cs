namespace TORSEPAN.Application.ProductionEvents.Queries.GetStageWorkload;

public sealed class GetStageWorkloadResponse
{
    public string Stage { get; set; } = string.Empty;

    public int Count { get; set; }
}