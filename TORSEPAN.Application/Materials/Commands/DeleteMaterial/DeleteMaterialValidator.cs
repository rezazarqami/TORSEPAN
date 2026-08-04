using FluentValidation;

namespace TORSEPAN.Application.Materials.Commands.DeleteMaterial;

public sealed class DeleteMaterialValidator
    : AbstractValidator<DeleteMaterialCommand>
{
    public DeleteMaterialValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}