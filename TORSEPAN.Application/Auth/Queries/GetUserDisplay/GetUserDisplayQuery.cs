using MediatR;
using TORSEPAN.Application.Auth.Queries.GetUserDisplayList;

namespace TORSEPAN.Application.Auth.Queries.GetUserDisplay;

public sealed record GetUserDisplayQuery(Guid UserId)
    : IRequest<UserDisplayDto?>;