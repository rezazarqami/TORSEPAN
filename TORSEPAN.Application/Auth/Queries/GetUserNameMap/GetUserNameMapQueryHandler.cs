using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserNameMap;

public sealed class GetUserNameMapQueryHandler
    : IRequestHandler<GetUserNameMapQuery, List<UserNameMapDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserNameMapQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserNameMapDto>> Handle(
        GetUserNameMapQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserNameMapDto
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName
            })
            .ToList();
    }
}