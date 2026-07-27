using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicInfo;

public sealed class GetUserPublicInfoQueryHandler
    : IRequestHandler<GetUserPublicInfoQuery, UserPublicInfoDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserPublicInfoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPublicInfoDto?> Handle(
        GetUserPublicInfoQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserPublicInfoDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}