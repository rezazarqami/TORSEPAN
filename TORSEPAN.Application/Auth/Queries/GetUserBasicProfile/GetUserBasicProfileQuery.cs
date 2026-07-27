using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserBasicProfile;

public sealed record GetUserBasicProfileQuery(Guid UserId)
    : IRequest<UserBasicProfileDto?>;