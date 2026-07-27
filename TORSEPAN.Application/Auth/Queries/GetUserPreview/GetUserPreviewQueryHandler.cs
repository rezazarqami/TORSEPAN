using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserPreview;

public sealed class GetUserPreviewQueryHandler
    : IRequestHandler<GetUserPreviewQuery, UserPreviewDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserPreviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPreviewDto?> Handle(
        GetUserPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserPreviewDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}