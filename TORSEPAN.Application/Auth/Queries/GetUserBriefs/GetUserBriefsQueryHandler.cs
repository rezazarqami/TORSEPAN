using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserBriefs;

public sealed class GetUserBriefsQueryHandler
    : IRequestHandler<GetUserBriefsQuery, List<UserBriefDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserBriefsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserBriefDto>> Handle(
        GetUserBriefsQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserBriefDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName
            })
            .ToList();
    }
}