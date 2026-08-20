namespace TORSEPAN.Panel.Models;

public sealed class ProductionOperationDto
{
    public int Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? DisplayTitle { get; set; }
}
