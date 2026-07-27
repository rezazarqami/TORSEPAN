using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetFinishedHandpans;

public sealed record GetFinishedHandpansQuery()
    : IRequest<IReadOnlyCollection<GetFinishedHandpansResponse>>;