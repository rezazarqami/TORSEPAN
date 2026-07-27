using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserExistsByFullName;

public sealed record GetUserExistsByFullNameQuery(string FullName)
    : IRequest<bool>;