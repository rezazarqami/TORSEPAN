using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed record GetProductionReportQuery()
    : IRequest<GetProductionReportResponse>;