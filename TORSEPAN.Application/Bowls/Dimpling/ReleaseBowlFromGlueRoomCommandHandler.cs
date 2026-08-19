using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class ReleaseBowlFromGlueRoomCommandHandler
    : IRequestHandler<ReleaseBowlFromGlueRoomCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    public ReleaseBowlFromGlueRoomCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        ReleaseBowlFromGlueRoomCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForFinalTune)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.GlueRoom)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);

        var assembly = (await _unitOfWork.HandpanAssemblies.FindAsync(
            x => x.TopBowlId == bowl.Id || x.BottomBowlId == bowl.Id)).SingleOrDefault();

        if (assembly is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        var pairedBowls = (await _unitOfWork.Bowls.FindAsync(
            x => x.Id == assembly.TopBowlId || x.Id == assembly.BottomBowlId)).ToList();

        if (pairedBowls.Count != 2 || pairedBowls.Any(x => x.Stage != ProductionStage.GlueRoom))
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        var topBowl = pairedBowls.Single(x => x.Id == assembly.TopBowlId);
        var handpan = await _unitOfWork.Handpans.GetBySerialNumberAsync(
            topBowl.ProductionCode);

        if (handpan is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        handpan.ChangeStatus(ProductionStatus.Waiting);
        handpan.ChangeStage(ProductionStage.WaitingForFinalTune);
        _unitOfWork.Handpans.Update(handpan);

        foreach (var pairedBowl in pairedBowls)
        {
            pairedBowl.MarkAsWaiting();
            pairedBowl.ChangeStage(ProductionStage.WaitingForFinalTune);
            _unitOfWork.Bowls.Update(pairedBowl);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(
            pairedBowls.Single(x => x.Id == bowl.Id)));
    }
}
