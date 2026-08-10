using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Materials.Commands.AdjustBowlStock;

public sealed class AdjustBowlStockHandler : IRequestHandler<AdjustBowlStockCommand>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts;
    public AdjustBowlStockHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts) { _unitOfWork = unitOfWork; _alerts = alerts; }

    public async Task Handle(AdjustBowlStockCommand request, CancellationToken cancellationToken)
    {
        var material = await _unitOfWork.Materials.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Material not found.");

        if ((int)material.Category != 4)
            throw new InvalidOperationException("This material is not a bowl material.");

        var previousTop = material.TopBowlQuantity; var previousBottom = material.BottomBowlQuantity;
        if (request.SetAbsolute)
            material.SetBowlStock(request.TopQuantity, request.BottomQuantity);
        else
            material.AddBowlStock(request.TopQuantity, request.BottomQuantity);

        _unitOfWork.Materials.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (material.TopBowlLowStockThreshold > 0 && previousTop >= material.TopBowlLowStockThreshold && material.TopBowlQuantity < material.TopBowlLowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "Ú©Ø§Ø³Ù‡ Ø±Ùˆ", material.TopBowlQuantity, material.TopBowlLowStockThreshold, cancellationToken);
        if (material.BottomBowlLowStockThreshold > 0 && previousBottom >= material.BottomBowlLowStockThreshold && material.BottomBowlQuantity < material.BottomBowlLowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "Ú©Ø§Ø³Ù‡ Ø²ÛŒØ±", material.BottomBowlQuantity, material.BottomBowlLowStockThreshold, cancellationToken);
    }
}

