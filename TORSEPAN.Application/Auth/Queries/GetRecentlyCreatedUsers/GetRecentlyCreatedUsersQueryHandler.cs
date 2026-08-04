using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetRecentlyCreatedUsers;

public sealed class GetRecentlyCreatedUsersQueryHandler
    : IRequestHandler<GetRecentlyCreatedUsersQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecentlyCreatedUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetRecentlyCreatedUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync())
            .OrderByDescending(x => x.Id)
            .Take(request.Count)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName,
                IsActive = x.IsActive,
                CreatedAt = DateTime.MinValue
            })
            .ToList();

        return users;
    }
}