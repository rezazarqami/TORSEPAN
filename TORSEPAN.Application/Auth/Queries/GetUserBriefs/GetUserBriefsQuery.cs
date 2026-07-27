using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserBriefs;

public sealed record GetUserBriefsQuery()
    : IRequest<List<UserBriefDto>>;