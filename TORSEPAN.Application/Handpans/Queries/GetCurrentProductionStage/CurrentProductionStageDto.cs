namespace TORSEPAN.Application.Handpans.Queries.GetCurrentProductionStage;

public sealed class CurrentProductionStageDto
{
    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}