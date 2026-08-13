using MediatR;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

public sealed record GetProductionReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    Guid? UserId = null,
    ProductionAction? Action = null,
    EventResult? Result = null) : IRequest<GetProductionReportResponse>;
