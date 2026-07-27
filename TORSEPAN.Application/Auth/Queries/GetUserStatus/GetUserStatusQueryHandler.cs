using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatus;

public sealed class GetUserStatusQueryHandler
    : IRequestHandler<GetUserStatusQuery, UserStatusDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserStatusDto?> Handle(
        GetUserStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserStatusDto
        {
            Id = user.Id,
            IsActive = user.IsActive
        };
    }
}