using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserActivation;

public sealed record GetUserActivationQuery(Guid UserId)
    : IRequest<UserActivationDto?>;