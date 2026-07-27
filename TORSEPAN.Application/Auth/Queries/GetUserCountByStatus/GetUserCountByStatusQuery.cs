using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserCountByStatus;

public sealed record GetUserCountByStatusQuery(bool IsActive)
    : IRequest<int>;