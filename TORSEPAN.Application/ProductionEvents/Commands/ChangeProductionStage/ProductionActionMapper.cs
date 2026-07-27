using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;

internal static class ProductionActionMapper
{
    public static ProductionAction Map(ProductionStage stage)
    {
        return stage switch
        {
            ProductionStage.Dimple => ProductionAction.Dimple,

            ProductionStage.Shape => ProductionAction.Shape,

            ProductionStage.HeatTreatment => ProductionAction.Furnace,

            ProductionStage.Glue => ProductionAction.Glue,

            ProductionStage.Tune => ProductionAction.Tune,

            ProductionStage.FinalTune => ProductionAction.FineTune,

            ProductionStage.QualityControl => ProductionAction.QualityCheck,

            ProductionStage.Packaging => ProductionAction.Packaging,

            ProductionStage.FinishedWarehouse => ProductionAction.WarehouseEntry,

            ProductionStage.Rejected => ProductionAction.Reject,

            _ => ProductionAction.Created
        };
    }
}