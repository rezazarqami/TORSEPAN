using MediatR;

namespace TORSEPAN.Application.Handpans.Queries.GetReadyForPackaging;

public sealed record GetReadyForPackagingQuery
    : IRequest<IReadOnlyList<HandpanDto>>;