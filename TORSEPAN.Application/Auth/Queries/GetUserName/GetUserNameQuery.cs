using MediatR;
using TORSEPAN.Application.Auth.Queries.GetUserIdNameList;

namespace TORSEPAN.Application.Auth.Queries.GetUserName;

public sealed record GetUserNameQuery(Guid UserId)
    : IRequest<UserNameDto?>;