using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteHandpanQualityControlCommandHandler
    : IRequestHandler<CompleteHandpanQualityControlCommand, Result<BowlDimpleDto>>
{
    private static readonly HashSet<string> ValidReasons = ["OutOfTune", "Appearance", "Other"];
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteHandpanQualityControlCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteHandpanQualityControlCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Approved &&
            (!ValidReasons.Contains(request.RejectionReason ?? string.Empty) ||
             (request.RejectionReason == "Other" && string.IsNullOrWhiteSpace(request.Details))))
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);
        }

        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(x => x.ProductionCode == code)).SingleOrDefault();
        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if (bowl.Stage != ProductionStage.WaitingForQualityControl)
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

        var targetStage = request.Approved
            ? ProductionStage.FinishedWarehouse
            : ProductionStage.WaitingForFinalTune;

        handpan.ChangeStage(targetStage);
        handpan.ChangeStatus(request.Approved ? ProductionStatus.Completed : ProductionStatus.Waiting);
        _unitOfWork.Handpans.Update(handpan);

        foreach (var item in bowls)
        {
            item.ChangeStage(targetStage);
            if (request.Approved)
                item.CompleteProduction();
            else
                item.MarkAsWaiting();
            _unitOfWork.Bowls.Update(item);
        }

        var reasonText = request.RejectionReason switch
        {
            "OutOfTune" => "کوک نبودن",
            "Appearance" => "وضعیت ظاهری",
            "Other" => "سایر موارد",
            _ => string.Empty
        };
        var description = request.Approved
            ? "QC تأیید شد و ساز به انبار منتقل شد"
            : $"QC تأیید نشد؛ دلیل: {reasonText}; توضیحات: {request.Details}";

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpan.Id, assembly.Id, null, userId, ProductionAction.QualityCheck,
            request.Approved ? EventResult.Completed : EventResult.Rejected,
            null, description));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(
            bowls.Single(x => x.Id == bowl.Id)));
    }
}
