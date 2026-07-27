using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUsersAlphabetically;

public sealed record GetUsersAlphabeticallyQuery()
    : IRequest<List<UserDto>>;