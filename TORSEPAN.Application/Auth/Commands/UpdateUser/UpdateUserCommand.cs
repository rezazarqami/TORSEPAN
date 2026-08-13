using MediatR;

namespace TORSEPAN.Application.Auth.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string UserName,
    string FullName,
    string Title,
    List<Guid> RoleIds) : IRequest;
