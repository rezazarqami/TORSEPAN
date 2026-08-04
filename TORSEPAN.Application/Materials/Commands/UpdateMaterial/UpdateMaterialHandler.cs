using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.UpdateMaterial;

public sealed class UpdateMaterialHandler
    : IRequestHandler<UpdateMaterialCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMaterialHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        UpdateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id);

        if (material is null)
            return;

        material.Rename(request.Name);

        _unitOfWork.Materials.Update(material);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}