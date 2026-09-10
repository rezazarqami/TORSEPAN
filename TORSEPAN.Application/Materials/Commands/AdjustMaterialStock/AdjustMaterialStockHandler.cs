using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;

public sealed class AdjustMaterialStockHandler : IRequestHandler<AdjustMaterialStockCommand, int>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts; private readonly IUserContext _user;

    public AdjustMaterialStockHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts, IUserContext user) { _unitOfWork = unitOfWork; _alerts = alerts; _user = user; }

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
        var delta = material.Quantity - previous;
        if (delta != 0 && _user.UserId is Guid userId)
            await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null,null,null,userId,
                ProductionAction.WarehouseEntry,EventResult.Completed,null,
                MaterialStockMetadata.Encode(material.Id,material.Name,"general",delta,material.Quantity,
                    request.SetAbsolute ? "اصلاح موجودی" : "ورود به انبار")));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (material.LowStockThreshold > 0 && previous >= material.LowStockThreshold && material.Quantity < material.LowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "موجودی", material.Quantity, material.LowStockThreshold, cancellationToken);
        return material.Quantity;
    }
}
