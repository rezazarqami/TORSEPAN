using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Materials.Commands.AdjustBowlStock;

public sealed class AdjustBowlStockHandler : IRequestHandler<AdjustBowlStockCommand>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts; private readonly IUserContext _user;
    public AdjustBowlStockHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts, IUserContext user) { _unitOfWork = unitOfWork; _alerts = alerts; _user = user; }

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
        if (_user.UserId is Guid userId)
        {
            var topDelta=material.TopBowlQuantity-previousTop;var bottomDelta=material.BottomBowlQuantity-previousBottom;
            if(topDelta!=0)await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null,null,null,userId,ProductionAction.WarehouseEntry,EventResult.Completed,null,MaterialStockMetadata.Encode(material.Id,material.Name,"top",topDelta,material.TopBowlQuantity,request.SetAbsolute?"اصلاح موجودی":"ورود به انبار")));
            if(bottomDelta!=0)await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null,null,null,userId,ProductionAction.WarehouseEntry,EventResult.Completed,null,MaterialStockMetadata.Encode(material.Id,material.Name,"bottom",bottomDelta,material.BottomBowlQuantity,request.SetAbsolute?"اصلاح موجودی":"ورود به انبار")));
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (material.TopBowlLowStockThreshold > 0 && previousTop >= material.TopBowlLowStockThreshold && material.TopBowlQuantity < material.TopBowlLowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "کاسه رو", material.TopBowlQuantity, material.TopBowlLowStockThreshold, cancellationToken);
        if (material.BottomBowlLowStockThreshold > 0 && previousBottom >= material.BottomBowlLowStockThreshold && material.BottomBowlQuantity < material.BottomBowlLowStockThreshold)
            await _alerts.SendLowStockAsync(material.Name, "کاسه زیر", material.BottomBowlQuantity, material.BottomBowlLowStockThreshold, cancellationToken);
    }
}
