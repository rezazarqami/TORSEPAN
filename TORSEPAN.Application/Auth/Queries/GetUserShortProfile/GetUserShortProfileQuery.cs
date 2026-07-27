using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserShortProfile;

public sealed record GetUserShortProfileQuery(Guid UserId)
    : IRequest<UserShortProfileDto?>;