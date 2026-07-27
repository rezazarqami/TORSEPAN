using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatusLookup;

public sealed record GetUserStatusLookupQuery()
    : IRequest<List<UserStatusLookupDto>>;