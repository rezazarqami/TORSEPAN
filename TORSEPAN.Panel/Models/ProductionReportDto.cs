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
}
public sealed class ReportUserDto { public Guid Id { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; }
public sealed class UserPerformanceDto { public Guid UserId { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; public int OperationCount { get; set; } public int CompletedCount { get; set; } public int DurationMinutes { get; set; } }
public sealed class ProductionActivityDto { public Guid Id { get; set; } public DateTime EventDate { get; set; } public Guid UserId { get; set; } public string UserName { get; set; } = ""; public string FullName { get; set; } = ""; public int Action { get; set; } public string ActionTitle { get; set; } = ""; public int Result { get; set; } public string ResultTitle { get; set; } = ""; public int? DurationMinutes { get; set; } public string DurationTitle { get; set; } = ""; public string ProductionCode { get; set; } = ""; public string Description { get; set; } = ""; }
