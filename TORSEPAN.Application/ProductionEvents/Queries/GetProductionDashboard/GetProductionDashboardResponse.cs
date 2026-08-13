Exit code: 0
Wall time: 0.8 seconds
Output:
namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class GetProductionDashboardResponse
{
    public int TotalHandpans { get; set; }

    public int InProduction { get; set; }

    public int Finished { get; set; }

    public int Rejected { get; set; }

    public double CompletionRate { get; set; }

    public IReadOnlyList<ProductionQueueItemResponse> Queues { get; set; } = [];
    public IReadOnlyList<MonthlyUserOperationResponse> MonthlyUserOperations { get; set; } = [];
    public string CurrentPersianMonthTitle { get; set; } = string.Empty;
}

public sealed class MonthlyUserOperationResponse
{
    public string UserName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ProductionQueueItemResponse
{
    public string Stage { get; set; } = string.Empty;
    public IReadOnlyList<string> Codes { get; set; } = [];
}

