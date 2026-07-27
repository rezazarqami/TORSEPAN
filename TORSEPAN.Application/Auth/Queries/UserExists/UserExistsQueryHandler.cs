using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.UserExists;

public sealed class UserExistsQueryHandler
    : IRequestHandler<UserExistsQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserExistsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UserExistsQuery request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Users.ExistsAsync(request.UserId);
    }
}