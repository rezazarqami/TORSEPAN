using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserDetails;

public sealed record GetUserDetailsQuery(Guid UserId)
    : IRequest<UserDetailsDto?>;