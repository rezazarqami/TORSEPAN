using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed record GetProductionDashboardQuery(DateTime? SelectedDate = null)
    : IRequest<GetProductionDashboardResponse>;
