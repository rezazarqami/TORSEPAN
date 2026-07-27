using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUsersByStatus;

public sealed record GetUsersByStatusQuery(bool IsActive)
    : IRequest<List<UserDto>>;