using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Bowls.Dimpling;

internal static class BowlDimpleMapper
{
    public static BowlDimpleDto Map(Bowl bowl) => new()
    {
        Id = bowl.Id,
        ProductionCode = ProductionCodeNormalizer.Normalize(bowl.ProductionCode),
        BowlType = (int)bowl.BowlType,
        HasNotes = bowl.HasNotes,
        NoteCount = bowl.KnownNoteCount,
        InstrumentType = (int)bowl.InstrumentType,
        Status = (int)bowl.Status,
        Stage = (int)bowl.Stage,
        IsCustomScale = bowl.IsCustomScale
    };
}
