using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommand : IRequest<Result<Guid>>
{
    public BowlType BowlType { get; set; }

    public bool HasNotes { get; set; }

    public InstrumentType InstrumentType { get; set; }

    public int? NoteCount { get; set; }
}