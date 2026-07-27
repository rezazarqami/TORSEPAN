using FluentValidation;

namespace TORSEPAN.Application.Handpans.Commands.CreateHandpan;

public sealed class CreateHandpanCommandValidator
    : AbstractValidator<CreateHandpanCommand>
{
    public CreateHandpanCommandValidator()
    {
        RuleFor(x => x.AssemblyId)
            .NotEmpty();

        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}