using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserInfoCard;

public sealed record GetUserInfoCardQuery(Guid UserId)
    : IRequest<UserInfoCardDto?>;