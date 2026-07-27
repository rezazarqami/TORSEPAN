using FluentValidation;

namespace TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;

public sealed class ChangeProductionStageCommandValidator
    : AbstractValidator<ChangeProductionStageCommand>
{
    public ChangeProductionStageCommandValidator()
    {
        RuleFor(x => x.HandpanId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.NextStage)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}