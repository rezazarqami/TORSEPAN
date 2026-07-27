namespace TORSEPAN.Application.ProductionEvents.Queries.GetHandpanDetails;

public sealed class GetHandpanDetailsResponse
{
    public Guid Id { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}