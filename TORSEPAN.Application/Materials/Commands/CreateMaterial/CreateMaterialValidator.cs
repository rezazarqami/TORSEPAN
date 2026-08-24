using FluentValidation;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed class CreateMaterialValidator
    : AbstractValidator<CreateMaterialCommand>
{
    public CreateMaterialValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InitialTopBowlQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InitialBottomBowlQuantity).GreaterThanOrEqualTo(0);
    }
}
