namespace TORSEPAN.Panel.Models;

public sealed class ProductionReportDto
{
    public int TotalOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int RejectedOrFailedOperations { get; set; }
    public int TotalDurationMinutes { get; set; }
    public List<ReportUserDto> Users { get; set; } = [];
    public List<UserPerformanceDto> UserPerformance { get; set; } = [];
    public List<ProductionActivityDto> Activities { get; set; } = [];
    public List<ReportTrendDto> Trend { get; set; } = [];
    public List<ReportTrendDto> DurationTrend { get; set; } = [];
    public UserTrendSummaryDto? UserTrend { get; set; }
}
public sealed class ReportTrendDto { public string Label { get; set; }=""; public int Count { get; set; } public double Average { get; set; } }
public sealed class UserTrendSummaryDto { public int CurrentOperationCount { get; set; } public int PreviousOperationCount { get; set; } public double AverageOperationCount { get; set; } public int CurrentDurationMinutes { get; set; } public int PreviousDurationMinutes { get; set; } public double AverageDurationMinutes { get; set; } }
public sealed class ReportUserDto { public Guid Id { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; }
public sealed class UserPerformanceDto { public Guid UserId { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; public int OperationCount { get; set; } public int CompletedCount { get; set; } public int DurationMinutes { get; set; } public List<TimedOperationPerformanceDto> TimedOperations { get; set; } = []; public List<UntimedOperationPerformanceDto> UntimedOperations { get; set; } = []; }
public sealed class TimedOperationPerformanceDto { public int Action { get; set; } public string ActionTitle { get; set; } = ""; public int Count { get; set; } public int TotalDurationMinutes { get; set; } public double AverageDurationMinutes { get; set; } }
public sealed class UntimedOperationPerformanceDto { public int Action { get; set; } public string ActionTitle { get; set; } = ""; public int Count { get; set; } }
public sealed class ProductionActivityDto { public Guid Id { get; set; } public DateTime EventDate { get; set; } public Guid UserId { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; public int Action { get; set; } public string ActionTitle { get; set; } = ""; public int Result { get; set; } public string ResultTitle { get; set; } = ""; public int? DurationMinutes { get; set; } public string DurationTitle { get; set; } = ""; public string ProductionCode { get; set; } = ""; public string Description { get; set; } = ""; }
