using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserHeader;

public sealed record GetUserHeaderQuery(Guid UserId)
    : IRequest<UserHeaderDto?>;