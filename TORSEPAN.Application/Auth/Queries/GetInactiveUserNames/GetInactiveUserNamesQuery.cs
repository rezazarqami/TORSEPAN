using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetInactiveUserNames;

public sealed record GetInactiveUserNamesQuery()
    : IRequest<List<string>>;