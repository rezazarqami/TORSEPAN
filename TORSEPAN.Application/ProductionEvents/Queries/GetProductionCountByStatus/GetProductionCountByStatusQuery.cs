using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionCountByStatus;

public sealed record GetProductionCountByStatusQuery()
    : IRequest<GetProductionCountByStatusResponse>;