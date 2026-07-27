using MediatR;

namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed record GetAllHandpansQuery()
    : IRequest<IReadOnlyList<HandpanDto>>;