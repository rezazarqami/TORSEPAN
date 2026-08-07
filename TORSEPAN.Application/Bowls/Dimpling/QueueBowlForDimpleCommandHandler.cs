using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class QueueBowlForDimpleCommandHandler
    : IRequestHandler<QueueBowlForDimpleCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public QueueBowlForDimpleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        QueueBowlForDimpleCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForDimple)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.Created)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);

        bowl.StartProduction();
        bowl.ChangeStage(ProductionStage.WaitingForDimple);

        _unitOfWork.Bowls.Update(bowl);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
