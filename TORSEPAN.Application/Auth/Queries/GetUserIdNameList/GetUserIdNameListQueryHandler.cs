using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdNameList;

public sealed class GetUserIdNameListQueryHandler
    : IRequestHandler<GetUserIdNameListQuery, List<UserIdNameDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserIdNameListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserIdNameDto>> Handle(
        GetUserIdNameListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserIdNameDto
            {
                Id = x.Id,
                UserName = x.UserName
            })
            .ToList();
    }
}