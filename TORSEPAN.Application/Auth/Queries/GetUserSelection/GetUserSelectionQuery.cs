using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserSelection;

public sealed record GetUserSelectionQuery()
    : IRequest<List<UserSelectionDto>>;