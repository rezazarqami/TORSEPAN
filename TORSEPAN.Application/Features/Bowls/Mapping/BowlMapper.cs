using TORSEPAN.Application.Features.Bowls.DTOs;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Features.Bowls.Mapping;

public static class BowlMapper
{
    public static BowlDto ToDto(this Bowl bowl)
    {
        return new BowlDto
        {
            Id = bowl.Id,
            ProductionCode = bowl.ProductionCode,
            BowlType = bowl.BowlType,
            HasNotes = bowl.HasNotes,
            InstrumentType = bowl.InstrumentType,
            NoteCount = bowl.NoteCount,
            Status = bowl.Status,
            Stage = bowl.Stage
        };
    }
}