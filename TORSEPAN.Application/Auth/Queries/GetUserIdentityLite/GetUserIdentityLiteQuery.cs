using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdentityLite;

public sealed record GetUserIdentityLiteQuery(Guid UserId)
    : IRequest<UserIdentityLiteDto?>;