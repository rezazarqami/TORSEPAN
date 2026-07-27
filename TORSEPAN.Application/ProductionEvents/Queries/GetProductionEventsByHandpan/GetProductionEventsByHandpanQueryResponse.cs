using TORSEPAN.Application.ProductionEvents.DTOs;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventsByHandpan;

public sealed class GetProductionEventsByHandpanQueryResponse
{
    public Guid HandpanId { get; set; }

    public int TotalEvents { get; set; }

    public IReadOnlyCollection<ProductionEventDto> Events { get; set; }
        = Array.Empty<ProductionEventDto>();
}