using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUsersByStatus;

public sealed class GetUsersByStatusQueryHandler
    : IRequestHandler<GetUsersByStatusQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersByStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Where(x => x.IsActive == request.IsActive)
            .OrderBy(x => x.UserName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName,
                IsActive = x.IsActive
            })
            .ToList();
    }
}