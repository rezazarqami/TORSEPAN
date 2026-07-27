using MediatR;

namespace TORSEPAN.Application.Auth.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest;