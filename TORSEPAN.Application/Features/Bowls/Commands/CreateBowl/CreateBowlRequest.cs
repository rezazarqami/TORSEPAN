using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public class CreateBowlRequest
{
    public BowlType BowlType { get; set; }

    public bool HasNotes { get; set; }

    public InstrumentType InstrumentType { get; set; }

    public int? NoteCount { get; set; }
}