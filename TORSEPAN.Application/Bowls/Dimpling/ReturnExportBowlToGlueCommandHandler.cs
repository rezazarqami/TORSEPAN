using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;
public sealed class ReturnExportBowlToGlueCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ReturnExportBowlToGlueCommand, Result<BowlDimpleDto>>
{
    public async Task<Result<BowlDimpleDto>> Handle(ReturnExportBowlToGlueCommand request, CancellationToken ct)
    {
        var bowl=(await unitOfWork.Bowls.FindAsync(x=>x.ProductionCode==request.ProductionCode.Trim())).SingleOrDefault();
        if(bowl is null)return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if(bowl.Stage!=ProductionStage.WaitingForExportPackaging)return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        bowl.MarkAsWaiting(); bowl.ChangeStage(ProductionStage.WaitingForGlue); unitOfWork.Bowls.Update(bowl);
        var productionEvents = await unitOfWork.ProductionEvents.GetByBowlIdAsync(bowl.Id);
        foreach (var productionEvent in productionEvents)
        {
            if (productionEvent.ConvertExportTuneToNormalRoute())
                unitOfWork.ProductionEvents.Update(productionEvent);
        }
        await unitOfWork.SaveChangesAsync(ct); return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
