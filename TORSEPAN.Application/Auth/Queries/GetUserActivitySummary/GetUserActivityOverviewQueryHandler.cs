using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserActivityOverview;

public sealed class GetUserActivityOverviewQueryHandler
    : IRequestHandler<GetUserActivityOverviewQuery, UserActivityOverviewDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserActivityOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserActivityOverviewDto> Handle(
        GetUserActivityOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        return new UserActivityOverviewDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(x => x.IsActive),
            InactiveUsers = users.Count(x => !x.IsActive)
        };
    }
}