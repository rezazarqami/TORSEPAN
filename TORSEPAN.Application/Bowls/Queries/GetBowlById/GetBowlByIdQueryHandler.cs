using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Bowls.Queries.GetBowlById;

public sealed class GetBowlByIdQueryHandler
    : IRequestHandler<GetBowlByIdQuery, Result<BowlDetailDto>>
{
    private readonly IBowlQueryService _queryService;

    public GetBowlByIdQueryHandler(IBowlQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<BowlDetailDto>> Handle(
        GetBowlByIdQuery request,
        CancellationToken cancellationToken)
    {
        var bowl = await _queryService.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (bowl is null)
            return Result<BowlDetailDto>.Failure(ErrorCodes.BowlNotFound);

        return Result<BowlDetailDto>.Success(bowl);
    }
}