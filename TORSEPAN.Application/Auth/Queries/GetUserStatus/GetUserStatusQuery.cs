using MediatR;
using TORSEPAN.Application.Auth.Queries.GetUserStatusList;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatus;

public sealed record GetUserStatusQuery(Guid UserId)
    : IRequest<UserStatusDto?>;