using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullNames;

public sealed class GetUserFullNamesQueryHandler
    : IRequestHandler<GetUserFullNamesQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserFullNamesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(
        GetUserFullNamesQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => x.FullName)
            .ToList();
    }
}