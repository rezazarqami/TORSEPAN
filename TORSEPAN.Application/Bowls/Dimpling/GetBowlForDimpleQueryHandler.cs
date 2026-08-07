using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class GetBowlForDimpleQueryHandler
    : IRequestHandler<GetBowlForDimpleQuery, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBowlForDimpleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        GetBowlForDimpleQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        return bowl is null
            ? Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound)
            : Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
