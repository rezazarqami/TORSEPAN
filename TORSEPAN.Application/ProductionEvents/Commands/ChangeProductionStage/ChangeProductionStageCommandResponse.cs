namespace TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;

public sealed class ChangeProductionStageCommandResponse
{
    public Guid HandpanId { get; set; }

    public string PreviousStage { get; set; } = string.Empty;

    public string CurrentStage { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }
}