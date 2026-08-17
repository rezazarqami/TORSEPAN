using TORSEPAN.Application.Common.Pagination;

namespace TORSEPAN.Application.Bowls.Queries.GetAllBowls;

public sealed record GetAllBowlsQuery(
    PageRequest PageRequest, int? BowlType = null, bool? HasNotes = null,
    Guid? MaterialId = null, Guid? ScaleId = null, int? Stage = null)
    : PagedQuery<BowlDto>(PageRequest);
