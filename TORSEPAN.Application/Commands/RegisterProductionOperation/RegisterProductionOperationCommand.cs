using MediatR;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed record RegisterProductionOperationCommand(
    Guid UserId,
    string SerialNumber,
    EventResult Result,
    OperationDuration? Duration,
    string? Description) : IRequest<Guid>;