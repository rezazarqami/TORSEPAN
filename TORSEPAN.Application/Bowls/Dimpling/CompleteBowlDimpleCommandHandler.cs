using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlDimpleCommandHandler
    : IRequestHandler<CompleteBowlDimpleCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteBowlDimpleCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteBowlDimpleCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForShape)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.WaitingForDimple ||
            !Enum.IsDefined(request.Duration))
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        }

        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForShape);
        _unitOfWork.Bowls.Update(bowl);

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpanId: null,
            assemblyId: null,
            bowlId: bowl.Id,
            userId: userId,
            action: ProductionAction.Dimple,
            result: EventResult.Completed,
            duration: request.Duration,
            description: "Dimple completed"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
