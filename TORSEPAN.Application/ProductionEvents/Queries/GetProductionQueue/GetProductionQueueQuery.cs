using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionQueue;

public sealed record GetProductionQueueQuery(string Stage)
    : IRequest<IReadOnlyCollection<GetProductionQueueResponse>>;