using MediatR;

namespace TORSEPAN.Application.Auth.Queries.UserCount;

public sealed record UserCountQuery : IRequest<UserCountResponse>;