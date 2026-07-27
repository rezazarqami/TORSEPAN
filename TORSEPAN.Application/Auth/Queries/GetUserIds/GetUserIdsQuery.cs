using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserIds;

public sealed record GetUserIdsQuery()
    : IRequest<List<Guid>>;