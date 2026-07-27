using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserActivityOverview;

public sealed record GetUserActivityOverviewQuery()
    : IRequest<UserActivityOverviewDto>;