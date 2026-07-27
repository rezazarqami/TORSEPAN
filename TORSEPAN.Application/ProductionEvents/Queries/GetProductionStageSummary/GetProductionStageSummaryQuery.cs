using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionStageSummary;

public sealed record GetProductionStageSummaryQuery()
    : IRequest<IReadOnlyCollection<GetProductionStageSummaryResponse>>;