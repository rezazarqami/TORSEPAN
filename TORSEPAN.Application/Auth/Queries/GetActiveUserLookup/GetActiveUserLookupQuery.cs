using MediatR;
using TORSEPAN.Application.Auth.Queries.GetUserLookup;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUserLookup;

public sealed record GetActiveUserLookupQuery()
    : IRequest<List<UserLookupDto>>;