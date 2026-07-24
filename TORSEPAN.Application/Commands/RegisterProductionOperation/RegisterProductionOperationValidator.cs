using FluentValidation;
using TORSEPAN.Domain.Production;

namespace TORSEPAN.Application.Production.Commands.RegisterProductionOperation;

public sealed class RegisterProductionOperationValidator
    : AbstractValidator<RegisterProductionOperationCommand>
{
    public RegisterProductionOperationValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x)
            .Must(x =>
            {
                // فعلاً چون هنوز Handpan را نداریم،
                // فقط اعتبار اولیه انجام می‌شود.
                // اعتبارسنجی اصلی بعداً داخل Handler و بر اساس Workflow خواهد بود.
                return true;
            });
    }
}