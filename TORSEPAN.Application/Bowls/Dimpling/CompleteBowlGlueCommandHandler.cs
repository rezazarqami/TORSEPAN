using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlGlueCommandHandler
    : IRequestHandler<CompleteBowlGlueCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteBowlGlueCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteBowlGlueCommand request,
        CancellationToken cancellationToken)
    {
        var firstCode = request.ProductionCode.Trim();
        var secondCode = request.PairedProductionCode.Trim();

        if (string.IsNullOrWhiteSpace(secondCode) ||
            string.Equals(firstCode, secondCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);
        }

        var bowls = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == firstCode ||
                 x.ProductionCode == secondCode)).ToList();

        var first = bowls.SingleOrDefault(x => x.ProductionCode == firstCode);
        var second = bowls.SingleOrDefault(x => x.ProductionCode == secondCode);

        if (first is null || second is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (first.Stage == ProductionStage.GlueRoom)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(first));

        if (first.Stage != ProductionStage.WaitingForGlue ||
            second.Stage != ProductionStage.WaitingForGlue ||
            first.BowlType == second.BowlType ||
            first.InstrumentType != second.InstrumentType)
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);
        }

        var top = first.BowlType == BowlType.Top ? first : second;
        var bottom = first.BowlType == BowlType.Bottom ? first : second;

        var alreadyUsed = (await _unitOfWork.HandpanAssemblies.FindAsync(
            x => x.TopBowlId == top.Id || x.BottomBowlId == bottom.Id)).Any();

        if (alreadyUsed)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.Validation);

        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        var assembly = new HandpanAssembly(top.Id, bottom.Id);
        await _unitOfWork.HandpanAssemblies.AddAsync(assembly);

        var handpan = new Handpan(assembly.Id, top.ProductionCode);
        handpan.ChangeStatus(ProductionStatus.Waiting);
        handpan.ChangeStage(ProductionStage.GlueRoom);
        await _unitOfWork.Handpans.AddAsync(handpan);

        foreach (var bowl in bowls)
        {
            bowl.MarkAsWaiting();
            bowl.ChangeStage(ProductionStage.GlueRoom);
            _unitOfWork.Bowls.Update(bowl);

            await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
                handpanId: handpan.Id,
                assemblyId: assembly.Id,
                bowlId: bowl.Id,
                userId: userId,
                action: ProductionAction.Glue,
                result: EventResult.Completed,
                duration: null,
                description: $"Glued with bowl {(bowl.Id == first.Id ? secondCode : firstCode)}"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(first));
    }
}
