using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserCompact;

public sealed record GetUserCompactQuery(Guid UserId)
    : IRequest<UserCompactDto?>;