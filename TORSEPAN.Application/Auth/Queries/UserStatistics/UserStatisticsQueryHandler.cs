using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.UserStatistics;

public sealed class UserStatisticsQueryHandler
    : IRequestHandler<UserStatisticsQuery, UserStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserStatisticsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserStatisticsResponse> Handle(
        UserStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        int total = users.Count;
        int active = users.Where(u => u.IsActive).Count();
        int inactive = users.Where(u => !u.IsActive).Count();

        return new UserStatisticsResponse
        {
            TotalUsers = total,
            ActiveUsers = active,
            InactiveUsers = inactive,
            ActivePercentage = total == 0 ? 0 : Math.Round(active * 100.0 / total, 2),
            InactivePercentage = total == 0 ? 0 : Math.Round(inactive * 100.0 / total, 2)
        };
    }
}