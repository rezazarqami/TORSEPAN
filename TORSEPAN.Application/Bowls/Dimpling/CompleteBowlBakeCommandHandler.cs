using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlBakeCommandHandler
    : IRequestHandler<CompleteBowlBakeCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteBowlBakeCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteBowlBakeCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForTune)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.WaitingForBake)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);

        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForTune);
        _unitOfWork.Bowls.Update(bowl);

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpanId: null,
            assemblyId: null,
            bowlId: bowl.Id,
            userId: userId,
            action: ProductionAction.Furnace,
            result: EventResult.Completed,
            duration: null,
            description: "Bake completed"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
