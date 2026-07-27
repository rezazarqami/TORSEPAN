using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.SearchUsers;

public sealed class SearchUsersQueryHandler
    : IRequestHandler<SearchUsersQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var keyword = request.Keyword.Trim().ToLower();

        var users = (await _unitOfWork.Users.GetAllAsync())
            .Where(x =>
                x.UserName.ToLower().Contains(keyword) ||
                x.FullName.ToLower().Contains(keyword))
            .OrderBy(x => x.UserName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName,
                IsActive = x.IsActive
            })
            .ToList();

        return users;
    }
}