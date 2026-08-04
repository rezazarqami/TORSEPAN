using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.DeleteMaterial;

public sealed class DeleteMaterialHandler
    : IRequestHandler<DeleteMaterialCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMaterialHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        DeleteMaterialCommand request,
        CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id);

        if (material is null)
            return;

        _unitOfWork.Materials.Remove(material);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}