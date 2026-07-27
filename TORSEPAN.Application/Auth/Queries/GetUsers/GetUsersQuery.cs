using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<List<UserDto>>;