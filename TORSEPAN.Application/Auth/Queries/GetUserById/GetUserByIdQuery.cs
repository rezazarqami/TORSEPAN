using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId)
    : IRequest<UserDto>;