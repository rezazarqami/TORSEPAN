using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteHandpanPackagingCommandHandler
    : IRequestHandler<CompleteHandpanPackagingCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteHandpanPackagingCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteHandpanPackagingCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(x => x.ProductionCode == code)).SingleOrDefault();
        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if (bowl.Stage != ProductionStage.WaitingForPackaging)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);

        var assembly = (await _unitOfWork.HandpanAssemblies.FindAsync(
            x => x.TopBowlId == bowl.Id || x.BottomBowlId == bowl.Id)).SingleOrDefault();
        if (assembly is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        var bowls = (await _unitOfWork.Bowls.FindAsync(
            x => x.Id == assembly.TopBowlId || x.Id == assembly.BottomBowlId)).ToList();
        var top = bowls.SingleOrDefault(x => x.Id == assembly.TopBowlId);
        if (bowls.Count != 2 || top is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        var handpan = await _unitOfWork.Handpans.GetBySerialNumberAsync(top.ProductionCode);
        if (handpan is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);
        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        var selectedMaterialIds = request.MaterialIds.Distinct().ToArray();
        var selectedMaterials = selectedMaterialIds.Length == 0
            ? []
            : (await _unitOfWork.Materials.FindAsync(
                x => selectedMaterialIds.Contains(x.Id))).ToList();

        if (selectedMaterials.Count != selectedMaterialIds.Length ||
            selectedMaterials.Any(x => x.Category != MaterialCategory.Other || x.Quantity < 1))
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        foreach (var material in selectedMaterials)
        {
            material.TryConsume();
            _unitOfWork.Materials.Update(material);
        }

        handpan.ChangeStage(ProductionStage.FinishedWarehouse);
        handpan.ChangeStatus(ProductionStatus.Completed);
        _unitOfWork.Handpans.Update(handpan);

        foreach (var item in bowls)
        {
            item.ChangeStage(ProductionStage.FinishedWarehouse);
            item.CompleteProduction();
            _unitOfWork.Bowls.Update(item);
        }

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpan.Id, assembly.Id, null, userId, ProductionAction.Packaging,
            EventResult.Completed, null, "بسته‌بندی انجام شد و ساز وارد انبار شد"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(
            bowls.Single(x => x.Id == bowl.Id)));
    }
}
