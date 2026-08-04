using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
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
            BowlType = (int)bowl.BowlType,
            HasNotes = bowl.HasNotes,
            InstrumentType = (int)bowl.InstrumentType,
            Status = (int)bowl.Status,
            Stage = (int)bowl.Stage
        };
    }
}