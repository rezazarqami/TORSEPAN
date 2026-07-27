using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserMiniList;

public sealed record GetUserMiniListQuery()
    : IRequest<List<UserMiniDto>>;