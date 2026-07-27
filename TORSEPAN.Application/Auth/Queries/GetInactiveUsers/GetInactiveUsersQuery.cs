using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetInactiveUsers;

public sealed record GetInactiveUsers
    : IRequest<List<UserDto>>;