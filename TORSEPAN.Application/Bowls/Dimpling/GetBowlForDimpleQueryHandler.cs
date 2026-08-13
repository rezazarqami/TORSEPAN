Exit code: 0
Wall time: 0.7 seconds
Output:
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
        var code = ProductionCodeNormalizer.Normalize(request.ProductionCode);
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        var dto = BowlDimpleMapper.Map(bowl);
        var events = await _unitOfWork.ProductionEvents.GetReportAsync(null, null, null, null, null);
        dto.Notes.AddRange(events.Where(x => x.BowlId == bowl.Id && x.Description.StartsWith("NOTE:"))
            .OrderBy(x => x.EventDate)
            .Select(x => $"{(string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)}: {x.Description[5..]}"));
        return Result<BowlDimpleDto>.Success(dto);
    }
}

