using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserInfoCard;

public sealed class GetUserInfoCardQueryHandler
    : IRequestHandler<GetUserInfoCardQuery, UserInfoCardDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserInfoCardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserInfoCardDto?> Handle(
        GetUserInfoCardQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserInfoCardDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}