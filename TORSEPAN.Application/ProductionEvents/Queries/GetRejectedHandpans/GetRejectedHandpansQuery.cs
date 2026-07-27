using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetRejectedHandpans;

public sealed record GetRejectedHandpansQuery()
    : IRequest<IReadOnlyCollection<GetRejectedHandpansResponse>>;