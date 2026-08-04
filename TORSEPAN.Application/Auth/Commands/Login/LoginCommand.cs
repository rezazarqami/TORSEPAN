using MediatR;

namespace TORSEPAN.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string UserName,
    string Password)
    : IRequest<LoginResult>;