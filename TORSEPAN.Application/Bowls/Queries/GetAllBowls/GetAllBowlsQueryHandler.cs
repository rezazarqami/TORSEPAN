using MediatR;
using TORSEPAN.Application.Common.Pagination;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Bowls.Queries.GetAllBowls;

public sealed class GetAllBowlsQueryHandler
    : IRequestHandler<GetAllBowlsQuery, PagedResult<BowlDto>>
{
    private readonly IBowlQueryService _queryService;

    public GetAllBowlsQueryHandler(IBowlQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<PagedResult<BowlDto>> Handle(
        GetAllBowlsQuery request,
        CancellationToken cancellationToken)
    {
        return await _queryService.GetAllAsync(
            request.PageRequest, request.BowlType, request.HasNotes,
            request.MaterialId, request.ScaleId, request.Stage,
            cancellationToken);
    }
}
