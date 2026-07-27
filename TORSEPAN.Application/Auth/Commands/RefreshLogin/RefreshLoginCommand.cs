using MediatR;

namespace TORSEPAN.Application.Auth.Commands.RefreshLogin;

public sealed record RefreshLoginCommand(string RefreshToken)
    : IRequest<RefreshLoginResponse>;