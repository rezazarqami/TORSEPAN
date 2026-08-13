using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteHandpanFinalTuneCommandHandler
    : IRequestHandler<CompleteHandpanFinalTuneCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteHandpanFinalTuneCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteHandpanFinalTuneCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(x => x.ProductionCode == code)).SingleOrDefault();
        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForQualityControl)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
        if (bowl.Stage != ProductionStage.WaitingForFinalTune)
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

        handpan.ChangeStatus(ProductionStatus.Waiting);
        handpan.ChangeStage(ProductionStage.WaitingForQualityControl);
        _unitOfWork.Handpans.Update(handpan);

        foreach (var item in bowls)
        {
            item.MarkAsWaiting();
            item.ChangeStage(ProductionStage.WaitingForQualityControl);
            _unitOfWork.Bowls.Update(item);
        }

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpan.Id, assembly.Id, null, userId, ProductionAction.FineTune,
            EventResult.Completed, request.Duration, "Final tune completed"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(
            bowls.Single(x => x.Id == bowl.Id)));
    }
}
