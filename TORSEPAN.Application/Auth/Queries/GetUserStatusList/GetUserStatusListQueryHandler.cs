using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatusList;

public sealed class GetUserStatusListQueryHandler
    : IRequestHandler<GetUserStatusListQuery, List<UserStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserStatusListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserStatusDto>> Handle(
        GetUserStatusListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => new UserStatusDto
            {
                Id = x.Id,
                FullName = x.FullName,
                IsActive = x.IsActive
            })
            .ToList();
    }
}