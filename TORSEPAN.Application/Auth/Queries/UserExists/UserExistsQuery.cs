using MediatR;

namespace TORSEPAN.Application.Auth.Queries.UserExists;

public sealed record UserExistsQuery(Guid UserId)
    : IRequest<bool>;