using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserKeyValueList;

public sealed record GetUserKeyValueListQuery()
    : IRequest<List<UserKeyValueDto>>;