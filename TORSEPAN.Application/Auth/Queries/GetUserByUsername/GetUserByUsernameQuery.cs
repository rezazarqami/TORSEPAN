using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserByUsername;

public sealed record GetUserByUsernameQuery(string UserName)
    : IRequest<UserDto?>;