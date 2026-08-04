using FluentValidation;

namespace TORSEPAN.Application.Materials.Commands.UpdateMaterial;

public sealed class UpdateMaterialValidator
    : AbstractValidator<UpdateMaterialCommand>
{
    public UpdateMaterialValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}