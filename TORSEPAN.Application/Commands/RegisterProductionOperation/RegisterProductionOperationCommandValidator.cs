using FluentValidation;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationCommandValidator
    : AbstractValidator<RegisterProductionOperationCommand>
{
    public RegisterProductionOperationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Result)
            .IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}