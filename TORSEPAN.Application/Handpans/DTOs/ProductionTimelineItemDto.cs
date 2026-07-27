namespace TORSEPAN.Application.Handpans.DTOs;

public sealed class ProductionTimelineItemDto
{
    public Guid Id { get; set; }

    public string Operation { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime PerformedAt { get; set; }
}