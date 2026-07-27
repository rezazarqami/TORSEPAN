using FluentValidation;

namespace TORSEPAN.Application.ProductionEvents.Commands.CreateProductionEvent;

public sealed class CreateProductionEventCommandValidator
    : AbstractValidator<CreateProductionEventCommand>
{
    public CreateProductionEventCommandValidator()
    {
        RuleFor(x => x.HandpanId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotNull();

        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Result)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Duration)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Duration.HasValue);
    }
}