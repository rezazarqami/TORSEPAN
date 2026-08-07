using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Production;

public static class ProductionWorkflow
{
    private static readonly IReadOnlyList<ProductionTransition> _transitions =
    [
        // ایجاد
        new(
            ProductionStage.Created,
            ProductionAction.Created,
            ProductionStage.WaitingForDimple),

        // دیمپل
        new(
            ProductionStage.WaitingForDimple,
            ProductionAction.Dimple,
            ProductionStage.Dimple),

        new(
            ProductionStage.Dimple,
            ProductionAction.Dimple,
            ProductionStage.WaitingForShape),

        // شیپ
        new(
            ProductionStage.WaitingForShape,
            ProductionAction.Shape,
            ProductionStage.Shape),

        new(
            ProductionStage.Shape,
            ProductionAction.Shape,
            ProductionStage.WaitingForBake),

        // کوره
        new(
            ProductionStage.WaitingForBake,
            ProductionAction.Furnace,
            ProductionStage.Bake),

        new(
            ProductionStage.Bake,
            ProductionAction.Furnace,
            ProductionStage.WaitingForTune),

        // تیون
        new(
            ProductionStage.WaitingForTune,
            ProductionAction.Tune,
            ProductionStage.Tune),

        new(
            ProductionStage.Tune,
            ProductionAction.Tune,
            ProductionStage.WaitingForGlue),

        // چسب
        new(
            ProductionStage.WaitingForGlue,
            ProductionAction.Glue,
            ProductionStage.GlueRoom),

        new(
            ProductionStage.GlueRoom,
            ProductionAction.Glue,
            ProductionStage.WaitingForFinalTune),

        // فاین تیون
        new(
            ProductionStage.WaitingForFinalTune,
            ProductionAction.FineTune,
            ProductionStage.FinalTune),

        new(
            ProductionStage.FinalTune,
            ProductionAction.FineTune,
            ProductionStage.WaitingForQualityControl),

        // کنترل کیفیت
        new(
            ProductionStage.WaitingForQualityControl,
            ProductionAction.QualityCheck,
            ProductionStage.QualityControl),

        new(
            ProductionStage.QualityControl,
            ProductionAction.QualityCheck,
            ProductionStage.WaitingForPackaging),

        // بسته‌بندی
        new(
            ProductionStage.WaitingForPackaging,
            ProductionAction.Packaging,
            ProductionStage.Packaging),

        new(
            ProductionStage.Packaging,
            ProductionAction.Packaging,
            ProductionStage.FinishedWarehouse)
    ];

    public static IReadOnlyList<ProductionTransition> Transitions => _transitions;

    public static ProductionTransition? GetTransition(
        ProductionStage currentStage)
    {
        return _transitions.FirstOrDefault(x => x.CurrentStage == currentStage);
    }
}
