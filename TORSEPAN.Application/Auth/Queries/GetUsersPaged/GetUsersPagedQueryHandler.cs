using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUsersPaged;

public sealed class GetUsersPagedQueryHandler
    : IRequestHandler<GetUsersPagedQuery, GetUsersPagedResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersPagedQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetUsersPagedResponse> Handle(
        GetUsersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .ToList();

        return new GetUsersPagedResponse
        {
            TotalCount = users.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Items = users
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new TORSEPAN.Application.Auth.Queries.GetUsersPaged.UserDto
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    FullName = x.FullName,
                    IsActive = x.IsActive
                })
                .ToList()
        };
    }
}