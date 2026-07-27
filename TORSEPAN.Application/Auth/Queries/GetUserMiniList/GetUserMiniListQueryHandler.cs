using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserMiniList;

public sealed class GetUserMiniListQueryHandler
    : IRequestHandler<GetUserMiniListQuery, List<UserMiniDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserMiniListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserMiniDto>> Handle(
        GetUserMiniListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserMiniDto
            {
                Id = x.Id,
                UserName = x.UserName,
                IsActive = x.IsActive
            })
            .ToList();
    }
}