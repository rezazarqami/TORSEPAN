using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserNames;

public sealed class GetUserNamesQueryHandler
    : IRequestHandler<GetUserNamesQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserNamesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(
        GetUserNamesQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => x.UserName)
            .ToList();
    }
}