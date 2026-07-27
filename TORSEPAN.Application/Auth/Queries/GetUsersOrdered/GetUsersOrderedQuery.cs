using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUsersOrdered;

public sealed record GetUsersOrderedQuery(bool Descending = false)
    : IRequest<List<UserDto>>;