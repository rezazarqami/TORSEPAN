using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommandHandler : IRequestHandler<CreateBowlCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IInventoryAlertService _alerts;

    public CreateBowlCommandHandler(IUnitOfWork unitOfWork, IInventoryAlertService alerts)
    {
        _unitOfWork = unitOfWork;
        _alerts = alerts;
    }

    public async Task<Result<Guid>> Handle(
        CreateBowlCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Bowls.AnyAsync(
            b => b.ProductionCode.Trim() == request.ProductionCode.Trim(),
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
            request.ProductionCode.Trim(),
            request.BowlType,
            request.HasNotes,
            request.InstrumentType,
            request.MaterialId);

        await _unitOfWork.Bowls.AddAsync(bowl);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var threshold = isTop ? material.TopBowlLowStockThreshold : material.BottomBowlLowStockThreshold;
        var current = isTop ? material.TopBowlQuantity : material.BottomBowlQuantity;
        if (threshold > 0 && previous > threshold && current <= threshold)
            await _alerts.SendLowStockAsync(material.Name, isTop ? "کاسه رو" : "کاسه زیر", current, threshold, cancellationToken);

        return Result<Guid>.Success(bowl.Id);
    }
}
