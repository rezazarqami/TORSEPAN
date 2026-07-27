using MediatR;

namespace TORSEPAN.Application.Auth.Queries.Login;

public sealed record LoginQuery(
    string UserName,
    string Password)
    : IRequest<LoginResponse>;