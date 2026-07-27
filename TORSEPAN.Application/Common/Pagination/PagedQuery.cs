using MediatR;

namespace TORSEPAN.Application.Common.Pagination;

public abstract record PagedQuery<TResponse>(
    PageRequest PageRequest)
    : IRequest<PagedResult<TResponse>>;