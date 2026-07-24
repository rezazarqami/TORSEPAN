using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Production;

public sealed class ProductionEngine : IProductionEngine
{
    public ProductionTransition? GetTransition(
        ProductionStage currentStage)
    {
        return ProductionWorkflow.GetTransition(currentStage);
    }

    public bool CanMoveTo(
        ProductionStage currentStage,
        ProductionStage nextStage)
    {
        var transition = GetTransition(currentStage);

        return transition is not null &&
               transition.NextStage == nextStage;
    }

    public void MoveTo(
        Handpan handpan,
        ProductionStage nextStage)
    {
        if (handpan is null)
            throw new ArgumentNullException(nameof(handpan));

        var transition = GetTransition(handpan.Stage);

        if (transition is null)
            throw new InvalidOperationException(
                $"No workflow defined for '{handpan.Stage}'.");

        if (transition.NextStage != nextStage)
            throw new InvalidOperationException(
                $"Cannot move production from '{handpan.Stage}' to '{nextStage}'.");

        handpan.ChangeStage(transition.NextStage);
    }
}