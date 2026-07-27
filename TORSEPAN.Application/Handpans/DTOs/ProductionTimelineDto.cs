namespace TORSEPAN.Application.Handpans.DTOs;

public sealed class ProductionTimelineDto
{
    public Guid HandpanId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string CurrentStage { get; set; } = string.Empty;

    public List<ProductionTimelineItemDto> Operations { get; set; }
        = new();
}