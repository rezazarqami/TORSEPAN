using MediatR;

namespace TORSEPAN.Application.Handpans.Queries.GetCurrentProductionStage;

public sealed record GetCurrentProductionStageQuery(
    string SerialNumber)
    : IRequest<CurrentProductionStageDto>;