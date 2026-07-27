using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserLookup;

public sealed record GetUserLookupQuery()
    : IRequest<List<UserLookupDto>>;