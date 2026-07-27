using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetStageWorkload;

public sealed record GetStageWorkloadQuery()
    : IRequest<IReadOnlyCollection<GetStageWorkloadResponse>>;