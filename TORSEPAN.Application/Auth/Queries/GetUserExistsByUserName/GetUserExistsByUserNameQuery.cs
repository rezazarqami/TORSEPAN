using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserExistsByUserName;

public sealed record GetUserExistsByUserNameQuery(string UserName)
    : IRequest<bool>;