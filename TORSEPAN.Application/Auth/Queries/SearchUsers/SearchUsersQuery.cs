using MediatR;

namespace TORSEPAN.Application.Auth.Queries.SearchUsers;

public sealed record SearchUsersQuery(string Keyword)
    : IRequest<List<UserDto>>;