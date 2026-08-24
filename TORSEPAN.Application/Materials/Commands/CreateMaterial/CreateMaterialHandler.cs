using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Materials.Commands.CreateMaterial;

public sealed class CreateMaterialHandler
    : IRequestHandler<CreateMaterialCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateMaterialHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        var category = Enum.IsDefined(typeof(MaterialCategory), request.Category)
            ? (MaterialCategory)request.Category
            : MaterialCategory.Other;
        var material = new Material(request.Name.Trim(), category,
            category == MaterialCategory.BowlMaterial ? 0 : request.InitialQuantity);
        if (category == MaterialCategory.BowlMaterial &&
            (request.InitialTopBowlQuantity > 0 || request.InitialBottomBowlQuantity > 0))
            material.AddBowlStock(request.InitialTopBowlQuantity, request.InitialBottomBowlQuantity);

        await _unitOfWork.Materials.AddAsync(material);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return material.Id;
    }
}
