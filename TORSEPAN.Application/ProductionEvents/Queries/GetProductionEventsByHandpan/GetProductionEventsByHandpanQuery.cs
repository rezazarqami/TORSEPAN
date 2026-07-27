using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventsByHandpan;

public sealed record GetProductionEventsByHandpanQuery(Guid HandpanId)
    : IRequest<GetProductionEventsByHandpanQueryResponse>;