using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUsersPaged;

public sealed record GetUsersPagedQuery(
    int PageNumber = 1,
    int PageSize = 10)
    : IRequest<GetUsersPagedResponse>;