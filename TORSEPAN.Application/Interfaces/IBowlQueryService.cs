using TORSEPAN.Application.Bowls.Queries.GetAllBowls;
using TORSEPAN.Application.Bowls.Queries.GetBowlById;
using TORSEPAN.Application.Common.Pagination;

namespace TORSEPAN.Application.Interfaces;

public interface IBowlQueryService
{
    Task<PagedResult<BowlDto>> GetAllAsync(
        PageRequest pageRequest, int? bowlType, bool? hasNotes,
        Guid? materialId, Guid? scaleId, int? stage,
        CancellationToken cancellationToken);

    Task<BowlDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}
