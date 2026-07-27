using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicInfo;

public sealed record GetUserPublicInfoQuery(Guid UserId)
    : IRequest<UserPublicInfoDto?>;