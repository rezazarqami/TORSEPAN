using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserLabel;

public sealed record GetUserLabelQuery(Guid UserId)
    : IRequest<UserLabelDto?>;