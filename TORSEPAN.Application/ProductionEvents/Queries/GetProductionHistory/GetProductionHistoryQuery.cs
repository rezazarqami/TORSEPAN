using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionHistory;

public sealed record GetProductionHistoryQuery(Guid HandpanId)
    : IRequest<IReadOnlyCollection<GetProductionHistoryResponse>>;