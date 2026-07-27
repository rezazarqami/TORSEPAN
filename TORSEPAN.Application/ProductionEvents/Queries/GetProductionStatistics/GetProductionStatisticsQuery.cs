using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStatistics;

public sealed record GetProductionStatisticsQuery()
    : IRequest<GetProductionStatisticsResponse>;