namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed record RegisterProductionOperationResult(
    Guid HandpanId,
    string SerialNumber,
    string CurrentStage,
    string NextStage,
    string Action);