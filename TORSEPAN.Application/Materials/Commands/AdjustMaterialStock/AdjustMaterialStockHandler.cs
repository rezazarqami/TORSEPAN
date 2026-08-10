using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;

public sealed class AdjustMaterialStockHandler : IRequestHandler<AdjustMaterialStockCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public AdjustMaterialStockHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<int> Handle(AdjustMaterialStockCommand request, CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Material not found.");

        if (request.SetAbsolute)
            material.SetStock(request.Quantity);
        else
            material.AddStock(request.Quantity);

        _unitOfWork.Materials.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return material.Quantity;
    }
}
