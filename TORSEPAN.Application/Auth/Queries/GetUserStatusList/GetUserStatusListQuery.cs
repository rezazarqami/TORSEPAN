using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatusList;

public sealed record GetUserStatusListQuery()
    : IRequest<List<UserStatusDto>>;