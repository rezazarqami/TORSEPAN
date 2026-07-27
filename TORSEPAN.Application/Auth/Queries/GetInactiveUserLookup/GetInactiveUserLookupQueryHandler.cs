using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetInactiveUserLookup;

public sealed class GetInactiveUserLookupQueryHandler
    : IRequestHandler<GetInactiveUserLookupQuery, List<UserLookupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInactiveUserLookupQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserLookupDto>> Handle(
        GetInactiveUserLookupQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Where(x => !x.IsActive)
            .OrderBy(x => x.UserName)
            .Select(x => new UserLookupDto
            {
                Id = x.Id,
                UserName = x.UserName
            })
            .ToList();
    }
}