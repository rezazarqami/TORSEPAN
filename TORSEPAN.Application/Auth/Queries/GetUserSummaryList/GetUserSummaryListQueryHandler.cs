using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserSummaryList;

public sealed class GetUserSummaryListQueryHandler
    : IRequestHandler<GetUserSummaryListQuery, List<UserSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSummaryListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserSummaryDto>> Handle(
        GetUserSummaryListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => new UserSummaryDto
            {
                Id = x.Id,
                FullName = x.FullName,
                UserName = x.UserName,
                IsActive = x.IsActive
            })
            .ToList();
    }
}