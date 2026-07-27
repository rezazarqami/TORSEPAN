using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserDisplayList;

public sealed class GetUserDisplayListQueryHandler
    : IRequestHandler<GetUserDisplayListQuery, List<UserDisplayDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserDisplayListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDisplayDto>> Handle(
        GetUserDisplayListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserDisplayDto
            {
                Id = x.Id,
                DisplayName = $"{x.FullName} ({x.UserName})"
            })
            .ToList();
    }
}