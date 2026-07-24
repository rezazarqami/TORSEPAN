using TORSEPAN.Application.Common.Pagination;

namespace TORSEPAN.Application.Bowls.Queries.GetAllBowls;

public sealed record GetAllBowlsQuery(
    PageRequest PageRequest)
    : PagedQuery<BowlDto>(PageRequest);