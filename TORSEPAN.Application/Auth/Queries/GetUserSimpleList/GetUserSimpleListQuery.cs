using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserSimpleList;

public sealed record GetUserSimpleListQuery()
    : IRequest<List<UserSimpleDto>>;