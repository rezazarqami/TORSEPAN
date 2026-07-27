using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullName;

public sealed record GetUserFullNameQuery(Guid UserId)
    : IRequest<UserFullNameDto?>;