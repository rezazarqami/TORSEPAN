using MediatR;

namespace TORSEPAN.Application.Auth.Commands.UpdateUserStatus;

public sealed record UpdateUserStatusCommand(
    Guid UserId,
    bool IsActive) : IRequest;