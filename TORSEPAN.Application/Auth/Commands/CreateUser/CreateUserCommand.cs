using MediatR;

namespace TORSEPAN.Application.Auth.Commands.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string FullName,
    string Password,
    List<Guid> RoleIds)
    : IRequest<Guid>;