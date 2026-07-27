using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicProfile;

public sealed record GetUserPublicProfileQuery(Guid UserId)
    : IRequest<UserPublicProfileDto?>;