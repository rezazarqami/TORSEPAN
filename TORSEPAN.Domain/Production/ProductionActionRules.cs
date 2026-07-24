using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Production;

public static class ProductionActionRules
{
    public static bool RequiresDuration(this ProductionAction action)
    {
        return action switch
        {
            ProductionAction.Dimple => true,
            ProductionAction.Shape => true,
            ProductionAction.Tune => true,
            ProductionAction.FineTune => true,
            ProductionAction.QualityCheck => true,

            _ => false
        };
    }

    public static bool AllowsFailure(this ProductionAction action)
    {
        return action switch
        {
            ProductionAction.QualityCheck => true,

            _ => false
        };
    }

    public static bool RequiresDescription(this ProductionAction action)
    {
        return false;
    }
}