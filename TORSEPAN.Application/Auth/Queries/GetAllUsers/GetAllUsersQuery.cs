using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetAllUsers;

public sealed record GetAllUsersQuery()
    : IRequest<IReadOnlyList<UserListItemDto>>;