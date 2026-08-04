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
    }
}