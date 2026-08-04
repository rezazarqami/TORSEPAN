using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUsers;

public sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync();

        return users
            .OrderBy(x => x.UserName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName,
                IsActive = x.IsActive,
                Role = string.Empty,
                Roles = x.UserRoles
                    .Select(r => r.Role.DisplayName)
                    .OrderBy(r => r)
                    .ToList()
            })
            .ToList();
    }
}