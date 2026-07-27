using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullNames;

public sealed record GetUserFullNamesQuery()
    : IRequest<List<string>>;