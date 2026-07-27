using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserBasicProfile;

public sealed class GetUserBasicProfileQueryHandler
    : IRequestHandler<GetUserBasicProfileQuery, UserBasicProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserBasicProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserBasicProfileDto?> Handle(
        GetUserBasicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserBasicProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}