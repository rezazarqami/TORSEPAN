using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserLookup;

public sealed class GetUserLookupQueryHandler
    : IRequestHandler<GetUserLookupQuery, List<UserLookupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserLookupQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserLookupDto>> Handle(
        GetUserLookupQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserLookupDto
            {
                Id = x.Id,
                UserName = x.UserName
            })
            .ToList();
    }
}