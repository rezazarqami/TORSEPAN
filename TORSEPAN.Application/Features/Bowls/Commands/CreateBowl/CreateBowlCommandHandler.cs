using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Materials;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommandHandler : IRequestHandler<CreateBowlCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts; private readonly IUserContext _user;

    public CreateBowlCommandHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts, IUserContext user)
    {
        _unitOfWork = unitOfWork;
        _alerts = alerts;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(
        CreateBowlCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedCode = ProductionCodeNormalizer.Normalize(request.ProductionCode);
        var exists = await _unitOfWork.Bowls.AnyAsync(
            b => b.ProductionCode.Trim() == normalizedCode,
            cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                new Error(
                    "ProductionCode",
                    "کد تولید قبلاً ثبت شده است."));
        }

        var material = await _unitOfWork.Materials.GetByIdAsync(request.MaterialId);
        var isTop = request.BowlType == BowlType.Top;
        var previous = isTop ? material?.TopBowlQuantity ?? 0 : material?.BottomBowlQuantity ?? 0;
        if (material is null || (int)material.Category != 4 ||
            !material.TryConsumeBowl(request.BowlType == BowlType.Top))
        {
            return Result<Guid>.Failure(new Error(
                "BowlStock",
                "موجودی کاسه انتخاب‌شده برای این متریال کافی نیست."));
        }

        _unitOfWork.Materials.Update(material);

        var bowl = new Bowl(
            normalizedCode,
            request.BowlType,
            request.HasNotes,
            request.InstrumentType,
            request.MaterialId);

        var current = isTop ? material.TopBowlQuantity : material.BottomBowlQuantity;
        await _unitOfWork.Bowls.AddAsync(bowl);
        if(_user.UserId is Guid userId)await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null,null,bowl.Id,userId,ProductionAction.Created,EventResult.Completed,null,MaterialStockMetadata.Encode(material.Id,material.Name,isTop?"top":"bottom",-1,current,"مصرف برای ساخت کاسه")));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var threshold = isTop ? material.TopBowlLowStockThreshold : material.BottomBowlLowStockThreshold;
        if (threshold > 0 && previous >= threshold && current < threshold)
            await _alerts.SendLowStockAsync(material.Name, isTop ? "کاسه رو" : "کاسه زیر", current, threshold, cancellationToken);

        return Result<Guid>.Success(bowl.Id);
    }
}
