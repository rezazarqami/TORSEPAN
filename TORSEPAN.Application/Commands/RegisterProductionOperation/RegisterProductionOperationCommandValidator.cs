using FluentValidation;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationCommandValidator
    : AbstractValidator<RegisterProductionOperationCommand>
{
    public RegisterProductionOperationCommandValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Duration)
            .IsInEnum()
            .When(x => x.Duration.HasValue);
    }
}