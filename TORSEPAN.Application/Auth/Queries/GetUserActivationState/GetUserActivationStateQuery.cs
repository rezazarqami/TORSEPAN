using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserActivationState;

public sealed record GetUserActivationStateQuery(Guid UserId)
    : IRequest<UserActivationStateDto?>;