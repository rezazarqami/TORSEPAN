using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetRecentlyCreatedUsers;

public sealed record GetRecentlyCreatedUsersQuery(int Count = 10)
    : IRequest<List<UserDto>>;