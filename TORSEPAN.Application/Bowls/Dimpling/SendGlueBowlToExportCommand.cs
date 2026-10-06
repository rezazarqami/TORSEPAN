using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record SendGlueBowlToExportCommand(string ProductionCode) : IRequest<Result<BowlDimpleDto>>;

public sealed class SendGlueBowlToExportCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SendGlueBowlToExportCommand, Result<BowlDimpleDto>>
{
    public async Task<Result<BowlDimpleDto>> Handle(SendGlueBowlToExportCommand request, CancellationToken ct)
    {
        var bowl = (await unitOfWork.Bowls.FindAsync(x => x.ProductionCode == request.ProductionCode.Trim())).SingleOrDefault();
        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        if (bowl.Stage != ProductionStage.WaitingForGlue)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForExportPackaging);
        unitOfWork.Bowls.Update(bowl);
        // Preserve the original tuner and duration; this is a route correction, not another tune.
        var events = await unitOfWork.ProductionEvents.GetByBowlIdAsync(bowl.Id);
        var tune = events.Where(x => x.Action == ProductionAction.Tune && x.Result == EventResult.Completed)
            .OrderByDescending(x => x.EventDate).FirstOrDefault();
        if (tune?.ConvertNormalTuneToExportRoute() == true) unitOfWork.ProductionEvents.Update(tune);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
