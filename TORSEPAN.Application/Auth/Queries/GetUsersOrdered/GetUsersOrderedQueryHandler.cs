using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUsersOrdered;

public sealed class GetUsersOrderedQueryHandler
    : IRequestHandler<GetUsersOrderedQuery, List<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersOrderedQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersOrderedQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        users = request.Descending
            ? users.OrderByDescending(x => x.UserName).ToList()
            : users.OrderBy(x => x.UserName).ToList();

        return users.Select(x => new UserDto
        {
            Id = x.Id,
            UserName = x.UserName,
            FullName = x.FullName,
            IsActive = x.IsActive
        }).ToList();
    }
}