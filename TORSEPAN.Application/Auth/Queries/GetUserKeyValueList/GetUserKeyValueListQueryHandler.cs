using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserKeyValueList;

public sealed class GetUserKeyValueListQueryHandler
    : IRequestHandler<GetUserKeyValueListQuery, List<UserKeyValueDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserKeyValueListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserKeyValueDto>> Handle(
        GetUserKeyValueListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserKeyValueDto
            {
                Key = x.Id,
                Value = x.UserName
            })
            .ToList();
    }
}