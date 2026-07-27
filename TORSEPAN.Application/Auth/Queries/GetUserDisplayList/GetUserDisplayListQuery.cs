using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserDisplayList;

public sealed record GetUserDisplayListQuery()
    : IRequest<List<UserDisplayDto>>;