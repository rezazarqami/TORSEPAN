using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserNameMap;

public sealed record GetUserNameMapQuery()
    : IRequest<List<UserNameMapDto>>;