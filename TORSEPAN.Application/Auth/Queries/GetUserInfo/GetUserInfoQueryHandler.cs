using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserInfo;

public sealed class GetUserInfoQueryHandler
    : IRequestHandler<GetUserInfoQuery, UserInfoDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserInfoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserInfoDto?> Handle(
        GetUserInfoQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserInfoDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}