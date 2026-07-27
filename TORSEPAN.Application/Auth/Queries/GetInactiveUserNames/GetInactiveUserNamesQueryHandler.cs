using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetInactiveUserNames;

public sealed class GetInactiveUserNamesQueryHandler
    : IRequestHandler<GetInactiveUserNamesQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInactiveUserNamesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(
        GetInactiveUserNamesQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Where(x => !x.IsActive)
            .OrderBy(x => x.UserName)
            .Select(x => x.UserName)
            .ToList();
    }
}