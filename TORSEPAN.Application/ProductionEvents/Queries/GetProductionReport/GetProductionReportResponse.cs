namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed class GetProductionReportResponse
{
    public int TotalHandpans { get; set; }

    public int TotalStages { get; set; }

    public IReadOnlyCollection<StageItem> Stages { get; set; }
        = Array.Empty<StageItem>();

    public sealed class StageItem
    {
        public string Stage { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}