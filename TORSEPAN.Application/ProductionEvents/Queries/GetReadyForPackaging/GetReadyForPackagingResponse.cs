namespace TORSEPAN.Application.ProductionEvents.Queries.GetReadyForPackaging;

public sealed class GetReadyForPackagingResponse
{
    public Guid Id { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}