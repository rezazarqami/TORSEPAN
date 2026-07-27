using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserOverview;

public sealed class GetUserOverviewQueryHandler
    : IRequestHandler<GetUserOverviewQuery, UserOverviewDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserOverviewDto?> Handle(
        GetUserOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserOverviewDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}