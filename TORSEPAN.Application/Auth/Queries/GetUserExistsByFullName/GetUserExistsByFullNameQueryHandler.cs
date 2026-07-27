using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserExistsByFullName;

public sealed class GetUserExistsByFullNameQueryHandler
    : IRequestHandler<GetUserExistsByFullNameQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserExistsByFullNameQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        GetUserExistsByFullNameQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Any(x => x.FullName == request.FullName);
    }
}