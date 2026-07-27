using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatisticsLite;

public sealed class GetUserStatisticsLiteQueryHandler
    : IRequestHandler<GetUserStatisticsLiteQuery, UserStatisticsLiteDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserStatisticsLiteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserStatisticsLiteDto> Handle(
        GetUserStatisticsLiteQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        return new UserStatisticsLiteDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(x => x.IsActive),
            InactiveUsers = users.Count(x => !x.IsActive)
        };
    }
}