using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUserNames;

public sealed record GetActiveUserNamesQuery()
    : IRequest<List<string>>;