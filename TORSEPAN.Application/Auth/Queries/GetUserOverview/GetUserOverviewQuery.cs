using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserOverview;

public sealed record GetUserOverviewQuery(Guid UserId)
    : IRequest<UserOverviewDto?>;