using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserExistsByUserName;

public sealed class GetUserExistsByUserNameQueryHandler
    : IRequestHandler<GetUserExistsByUserNameQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserExistsByUserNameQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        GetUserExistsByUserNameQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Any(x => x.UserName == request.UserName);
    }
}