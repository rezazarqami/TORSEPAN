using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public enum DashboardSection { All, Queues, Daily, Monthly }

public sealed record GetProductionDashboardQuery(DateTime? SelectedDate = null, DashboardSection Section = DashboardSection.All)
    : IRequest<GetProductionDashboardResponse>;
