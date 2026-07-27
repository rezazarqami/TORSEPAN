using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId)
    : IRequest<UserProfileDto?>;