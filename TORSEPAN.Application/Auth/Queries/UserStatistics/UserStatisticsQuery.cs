using MediatR;

namespace TORSEPAN.Application.Auth.Queries.UserStatistics;

public sealed record UserStatisticsQuery : IRequest<UserStatisticsResponse>;