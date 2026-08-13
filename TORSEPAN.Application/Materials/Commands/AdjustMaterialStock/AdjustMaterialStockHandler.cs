using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;

public sealed class AdjustMaterialStockHandler : IRequestHandler<AdjustMaterialStockCommand, int>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts;

    public AdjustMaterialStockHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts) { _unitOfWork = unitOfWork; _alerts = alerts; }

    public async Task<int> Handle(AdjustMaterialStockCommand request, CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Material not found.");

        var previous = material.Quantity;
        if (request.SetAbsolute)
            material.SetStock(request.Quantity);
        else
            material.AddStock(request.Quantity);

        _unitOfWork.Materials.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (material.LowStockThreshold > 0 && previous >= material.LowStockThreshold && material.Quantity < material.LowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "موجودی", material.Quantity, material.LowStockThreshold, cancellationToken);
        return material.Quantity;
    }
}
