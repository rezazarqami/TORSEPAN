using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Commands.CompleteProduction;

public sealed record CompleteProductionCommand(Guid HandpanId)
    : IRequest;