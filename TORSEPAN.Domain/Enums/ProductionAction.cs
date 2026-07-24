namespace TORSEPAN.Domain.Enums;

public enum ProductionAction
{
    Created = 1,

    // Bowl
    Dimple = 2,
    Shape = 3,
    Furnace = 4,

    // Assembly
    Glue = 5,

    // Handpan
    Tune = 6,
    FineTune = 7,
    QualityCheck = 8,
    Packaging = 9,
    WarehouseEntry = 10,

    // General
    Reject = 11
}