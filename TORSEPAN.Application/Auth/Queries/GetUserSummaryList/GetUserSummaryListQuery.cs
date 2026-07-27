using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserSummaryList;

public sealed record GetUserSummaryListQuery()
    : IRequest<List<UserSummaryDto>>;