using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed record GetProductionDashboardQuery()
    : IRequest<GetProductionDashboardResponse>;