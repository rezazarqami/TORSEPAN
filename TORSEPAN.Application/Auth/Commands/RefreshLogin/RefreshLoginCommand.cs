using MediatR;
using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.RefreshLogin;

public sealed record RefreshLoginCommand(string RefreshToken)
    : IRequest<RefreshLoginResponse>, IAllowAnonymousRequest;