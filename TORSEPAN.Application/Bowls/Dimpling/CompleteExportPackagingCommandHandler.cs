using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteExportPackagingCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<CompleteExportPackagingCommand, Result<BowlDimpleDto>>
{
    public async Task<Result<BowlDimpleDto>> Handle(CompleteExportPackagingCommand request, CancellationToken cancellationToken)
    {
        var bowl = (await unitOfWork.Bowls.FindAsync(x => x.ProductionCode == request.ProductionCode.Trim())).SingleOrDefault();
        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if (bowl.Stage != ProductionStage.WaitingForExportPackaging)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        if (userContext.UserId is not Guid userId) throw new UnauthorizedAccessException();

        bowl.ChangeStage(ProductionStage.ExportWarehouse);
        bowl.CompleteProduction();
        unitOfWork.Bowls.Update(bowl);
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null, null, bowl.Id, userId,
            ProductionAction.Packaging, EventResult.Completed, null, "Export packaging completed"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
