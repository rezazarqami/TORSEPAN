using MediatR;
using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string UserName,
    string Password)
    : IRequest<LoginResult>, IAllowAnonymousRequest;