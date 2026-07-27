namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStatistics;

public sealed class GetProductionStatisticsResponse
{
    public int TotalHandpans { get; set; }

    public int TotalCompletedHandpans { get; set; }

    public int TotalRejectedHandpans { get; set; }

    public int InProductionHandpans { get; set; }
}