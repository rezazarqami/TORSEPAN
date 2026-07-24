using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Production;

public sealed class ProductionTransition
{
    public ProductionStage CurrentStage { get; }

    public ProductionAction Action { get; }

    public ProductionStage NextStage { get; }

    public ProductionTransition(
        ProductionStage currentStage,
        ProductionAction action,
        ProductionStage nextStage)
    {
        CurrentStage = currentStage;
        Action = action;
        NextStage = nextStage;
    }
}