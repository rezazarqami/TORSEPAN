namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed class GetProductionReportResponse
{
    public int TotalOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int RejectedOrFailedOperations { get; set; }
    public int TotalDurationMinutes { get; set; }
    public IReadOnlyList<ReportUserItem> Users { get; set; } = [];
    public IReadOnlyList<UserPerformanceItem> UserPerformance { get; set; } = [];
    public IReadOnlyList<ProductionActivityItem> Activities { get; set; } = [];
}

public sealed class ReportUserItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class UserPerformanceItem
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public int CompletedCount { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class ProductionActivityItem
{
    public Guid Id { get; set; }
    public DateTime EventDate { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Action { get; set; }
    public string ActionTitle { get; set; } = string.Empty;
    public int Result { get; set; }
    public string ResultTitle { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public string DurationTitle { get; set; } = string.Empty;
    public string ProductionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
