using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicCard;

public sealed record GetUserPublicCardQuery(Guid UserId)
    : IRequest<UserPublicCardDto?>;