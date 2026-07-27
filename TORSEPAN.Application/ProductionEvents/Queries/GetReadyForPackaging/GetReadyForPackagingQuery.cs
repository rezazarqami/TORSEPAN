using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetReadyForPackaging;

public sealed record GetReadyForPackagingQuery()
    : IRequest<IReadOnlyCollection<GetReadyForPackagingResponse>>;