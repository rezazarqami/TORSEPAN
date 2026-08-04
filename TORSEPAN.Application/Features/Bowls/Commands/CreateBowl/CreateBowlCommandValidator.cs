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
    }
}