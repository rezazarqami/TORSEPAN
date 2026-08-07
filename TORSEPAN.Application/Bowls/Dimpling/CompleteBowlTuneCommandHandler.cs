using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlTuneCommandHandler
    : IRequestHandler<CompleteBowlTuneCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteBowlTuneCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteBowlTuneCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForGlue)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.WaitingForTune ||
            !Enum.IsDefined(request.Duration))
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        }

        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForGlue);
        _unitOfWork.Bowls.Update(bowl);

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpanId: null,
            assemblyId: null,
            bowlId: bowl.Id,
            userId: userId,
            action: ProductionAction.Tune,
            result: EventResult.Completed,
            duration: request.Duration,
            description: "Tune completed"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
