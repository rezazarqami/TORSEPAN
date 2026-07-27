using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserStatusLookup;

public sealed class GetUserStatusLookupQueryHandler
    : IRequestHandler<GetUserStatusLookupQuery, List<UserStatusLookupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserStatusLookupQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserStatusLookupDto>> Handle(
        GetUserStatusLookupQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .Select(x => new UserStatusLookupDto
            {
                Id = x.Id,
                UserName = x.UserName,
                IsActive = x.IsActive
            })
            .ToList();
    }
}