using FluentValidation;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommandValidator
    : AbstractValidator<CreateBowlCommand>
{
    public CreateBowlCommandValidator()
    {
        RuleFor(x => x.BowlType)
            .IsInEnum();

        RuleFor(x => x.InstrumentType)
            .IsInEnum();

        RuleFor(x => x.NoteCount)
            .NotNull()
            .When(x => x.HasNotes);

        RuleFor(x => x.NoteCount)
            .Null()
            .When(x => !x.HasNotes);

        RuleFor(x => x.NoteCount)
            .Equal(9)
            .When(x =>
                x.HasNotes &&
                x.InstrumentType == Domain.Enums.InstrumentType.Standard);

        RuleFor(x => x.NoteCount)
            .InclusiveBetween(10, 23)
            .When(x =>
                x.HasNotes &&
                x.InstrumentType == Domain.Enums.InstrumentType.Custom);
    }
}