using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetInactiveUserLookup;

public sealed record GetInactiveUserLookupQuery()
    : IRequest<List<UserLookupDto>>;