using MediatR;

namespace TORSEPAN.Application.Auth.Commands.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string FullName)
    : IRequest<Guid>;