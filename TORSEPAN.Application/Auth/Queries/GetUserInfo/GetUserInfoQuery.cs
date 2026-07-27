using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserInfo;

public sealed record GetUserInfoQuery(Guid UserId)
    : IRequest<UserInfoDto?>;