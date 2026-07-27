using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserSimpleList;

public sealed class GetUserSimpleListQueryHandler
    : IRequestHandler<GetUserSimpleListQuery, List<UserSimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSimpleListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserSimpleDto>> Handle(
        GetUserSimpleListQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => new UserSimpleDto
            {
                Id = x.Id,
                FullName = x.FullName
            })
            .ToList();
    }
}