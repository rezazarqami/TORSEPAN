using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserNames;

public sealed record GetUserNamesQuery()
    : IRequest<List<string>>;