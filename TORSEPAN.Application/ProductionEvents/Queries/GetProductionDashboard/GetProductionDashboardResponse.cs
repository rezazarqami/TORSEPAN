namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class GetProductionDashboardResponse
{
    public int TotalHandpans { get; set; }

    public int InProduction { get; set; }

    public int Finished { get; set; }

    public int Rejected { get; set; }

    public double CompletionRate { get; set; }

    public IReadOnlyList<ProductionQueueItemResponse> Queues { get; set; } = [];
}

public sealed class ProductionQueueItemResponse
{
    public string Stage { get; set; } = string.Empty;
    public IReadOnlyList<string> Codes { get; set; } = [];
}
