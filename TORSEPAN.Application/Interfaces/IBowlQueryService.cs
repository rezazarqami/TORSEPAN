using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Common.Pagination;

namespace TORSEPAN.Application.Interfaces;

public interface IBowlQueryService
{
    Task<PagedResult<BowlDto>> GetAllAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken);

    Task<BowlDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}