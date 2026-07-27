using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserBasicInfo;

public sealed class GetUserBasicInfoQueryHandler
    : IRequestHandler<GetUserBasicInfoQuery, List<UserBasicInfoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserBasicInfoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserBasicInfoDto>> Handle(
        GetUserBasicInfoQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => new UserBasicInfoDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName
            })
            .ToList();
    }
}