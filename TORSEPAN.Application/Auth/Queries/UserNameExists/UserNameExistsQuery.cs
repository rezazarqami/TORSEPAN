using MediatR;

namespace TORSEPAN.Application.Auth.Queries.UserNameExists;

public sealed record UserNameExistsQuery(string UserName)
    : IRequest<bool>;