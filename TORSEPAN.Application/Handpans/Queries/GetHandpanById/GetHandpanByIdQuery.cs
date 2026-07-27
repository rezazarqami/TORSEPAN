using MediatR;

namespace TORSEPAN.Application.Handpans.Queries.GetHandpanById;

public sealed record GetHandpanByIdQuery(Guid Id)
    : IRequest<HandpanDetailDto?>;