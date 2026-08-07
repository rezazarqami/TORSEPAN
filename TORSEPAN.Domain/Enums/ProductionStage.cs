namespace TORSEPAN.Domain.Enums;

public enum ProductionStage
{
    Created = 1,

    // Bowl Production
    WaitingForDimple = 2,
    Dimple = 3,

    WaitingForShape = 4,
    Shape = 5,

    WaitingForBake = 6,

    Bake = 7,

    // Handpan Production
    WaitingForTune = 8,
    Tune = 9,

    WaitingForGlue = 10,
    GlueRoom = 11,

    WaitingForFinalTune = 12,

    FinalTune = 13,

    WaitingForQualityControl = 14,
    QualityControl = 15,

    WaitingForPackaging = 16,
    Packaging = 17,

    FinishedWarehouse = 18,

    Rejected = 19
}
