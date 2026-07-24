using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Queries.GetBowlById;

public sealed record GetBowlByIdQuery(Guid Id)
    : IRequest<Result<BowlDetailDto>>;