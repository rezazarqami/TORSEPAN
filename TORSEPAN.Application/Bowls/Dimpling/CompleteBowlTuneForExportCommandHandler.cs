using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlTuneForExportCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<CompleteBowlTuneForExportCommand, Result<BowlDimpleDto>>
{
    public async Task<Result<BowlDimpleDto>> Handle(CompleteBowlTuneForExportCommand request, CancellationToken cancellationToken)
    {
        var bowl = (await unitOfWork.Bowls.FindAsync(x => x.ProductionCode == request.ProductionCode.Trim())).SingleOrDefault();
        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if (bowl.Stage != ProductionStage.WaitingForTune || !Enum.IsDefined(request.Duration))
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        if (userContext.UserId is not Guid userId) throw new UnauthorizedAccessException();

        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForExportPackaging);
        unitOfWork.Bowls.Update(bowl);
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null, null, bowl.Id, userId,
            ProductionAction.Tune, EventResult.Completed, request.Duration, "Tune completed - export package"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
