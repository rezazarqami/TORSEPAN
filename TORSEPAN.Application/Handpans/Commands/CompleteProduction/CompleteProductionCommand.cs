using MediatR;

namespace TORSEPAN.Application.Handpans.Commands.CompleteProduction;

public sealed record CompleteProductionCommand(Guid HandpanId)
    : IRequest;