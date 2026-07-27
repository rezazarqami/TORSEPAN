using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserSnapshot;

public sealed record GetUserSnapshotQuery(Guid UserId)
    : IRequest<UserSnapshotDto?>;