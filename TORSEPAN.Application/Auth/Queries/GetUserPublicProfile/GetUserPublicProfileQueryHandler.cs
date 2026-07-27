using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicProfile;

public sealed class GetUserPublicProfileQueryHandler
    : IRequestHandler<GetUserPublicProfileQuery, UserPublicProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserPublicProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPublicProfileDto?> Handle(
        GetUserPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserPublicProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}