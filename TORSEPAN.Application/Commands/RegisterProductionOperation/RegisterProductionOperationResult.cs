namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationResult
{
    public Guid HandpanId { get; init; }

    public string SerialNumber { get; init; } = string.Empty;

    public string CurrentStage { get; init; } = string.Empty;

    public string NextStage { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;
}