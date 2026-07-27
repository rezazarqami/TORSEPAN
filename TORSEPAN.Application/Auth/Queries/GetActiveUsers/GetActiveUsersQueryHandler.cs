using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUsers;

public sealed class GetActiveUsersQueryHandler
    : IRequestHandler<GetActiveUsers, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetActiveUsers request,
        CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync();

        return users
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName
            })
            .ToList();
    }
}