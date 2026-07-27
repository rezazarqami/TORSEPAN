using MediatR;

namespace TORSEPAN.Application.Auth.Queries.UserSummary;

public sealed record UserSummaryQuery : IRequest<UserSummaryResponse>;