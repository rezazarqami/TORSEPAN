using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.UserSummary;

public sealed class UserSummaryQueryHandler
    : IRequestHandler<UserSummaryQuery, UserSummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSummaryResponse> Handle(
        UserSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        return new UserSummaryResponse
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            InactiveUsers = users.Count(u => !u.IsActive)
        };
    }
}