using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUserNames;

public sealed class GetActiveUserNamesQueryHandler
    : IRequestHandler<GetActiveUserNamesQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveUserNamesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(
        GetActiveUserNamesQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Where(x => x.IsActive)
            .OrderBy(x => x.UserName)
            .Select(x => x.UserName)
            .ToList();
    }
}