using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUsersAlphabetically;

public sealed class GetUsersAlphabeticallyQueryHandler
    : IRequestHandler<GetUsersAlphabeticallyQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersAlphabeticallyQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersAlphabeticallyQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
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