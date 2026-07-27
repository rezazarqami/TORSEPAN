using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserIds;

public sealed class GetUserIdsQueryHandler
    : IRequestHandler<GetUserIdsQuery, List<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserIdsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Guid>> Handle(
        GetUserIdsQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => x.Id)
            .ToList();
    }
}