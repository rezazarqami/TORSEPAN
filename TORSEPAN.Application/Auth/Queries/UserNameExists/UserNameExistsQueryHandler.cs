using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.UserNameExists;

public sealed class UserNameExistsQueryHandler
    : IRequestHandler<UserNameExistsQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserNameExistsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UserNameExistsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        return user is not null;
    }
}