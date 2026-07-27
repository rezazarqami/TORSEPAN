using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserHealth;

public sealed record GetUserHealthQuery()
    : IRequest<UserHealthDto>;