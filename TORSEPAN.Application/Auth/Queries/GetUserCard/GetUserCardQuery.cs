using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserCard;

public sealed record GetUserCardQuery(Guid UserId)
    : IRequest<UserCardDto?>;