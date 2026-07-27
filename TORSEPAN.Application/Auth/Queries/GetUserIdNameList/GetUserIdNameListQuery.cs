using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdNameList;

public sealed record GetUserIdNameListQuery()
    : IRequest<List<UserIdNameDto>>;