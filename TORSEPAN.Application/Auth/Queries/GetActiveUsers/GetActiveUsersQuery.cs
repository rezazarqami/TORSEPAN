using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUsers;

public sealed record GetActiveUsers
    : IRequest<List<UserDto>>;