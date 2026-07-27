using MediatR;
using TORSEPAN.Application.Auth.Queries.GetUserStatusList;

namespace TORSEPAN.Application.Auth.Queries.GetUserState;

public sealed record GetUserStateQuery(Guid UserId)
    : IRequest<UserStateDto>;