using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdentity;

public sealed record GetUserIdentityQuery(Guid UserId)
    : IRequest<UserIdentityDto?>;