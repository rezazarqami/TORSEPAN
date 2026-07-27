using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetActiveUserLookup;

public sealed class GetActiveUserLookupQueryHandler
    : IRequestHandler<GetActiveUserLookupQuery, List<UserLookupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveUserLookupQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserLookupDto>> Handle(
        GetActiveUserLookupQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Where(x => x.IsActive)
            .OrderBy(x => x.UserName)
            .Select(x => new UserLookupDto
            {
                Id = x.Id,
                UserName = x.UserName
            })
            .ToList();
    }
}