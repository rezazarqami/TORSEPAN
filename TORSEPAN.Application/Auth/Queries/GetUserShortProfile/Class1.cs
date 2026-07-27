using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserShortProfile;

public sealed class GetUserShortProfileQueryHandler
    : IRequestHandler<GetUserShortProfileQuery, UserShortProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserShortProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserShortProfileDto?> Handle(
        GetUserShortProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserShortProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}