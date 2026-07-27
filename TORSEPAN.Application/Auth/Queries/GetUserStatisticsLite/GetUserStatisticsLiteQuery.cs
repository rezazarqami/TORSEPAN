using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatisticsLite;

public sealed record GetUserStatisticsLiteQuery()
    : IRequest<UserStatisticsLiteDto>;