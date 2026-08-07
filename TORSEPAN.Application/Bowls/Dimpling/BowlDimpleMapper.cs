using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Bowls.Dimpling;

internal static class BowlDimpleMapper
{
    public static BowlDimpleDto Map(Bowl bowl) => new()
    {
        Id = bowl.Id,
        ProductionCode = bowl.ProductionCode,
        BowlType = (int)bowl.BowlType,
        HasNotes = bowl.HasNotes,
        InstrumentType = (int)bowl.InstrumentType,
        Status = (int)bowl.Status,
        Stage = (int)bowl.Stage
    };
}
