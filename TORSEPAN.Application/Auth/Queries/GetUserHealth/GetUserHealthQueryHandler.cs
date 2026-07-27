using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserHealth;

public sealed class GetUserHealthQueryHandler
    : IRequestHandler<GetUserHealthQuery, UserHealthDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserHealthQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserHealthDto> Handle(
        GetUserHealthQuery request,
        CancellationToken cancellationToken)
    {
        var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

        return new UserHealthDto
        {
            HasUsers = users.Any(),
            HasActiveUsers = users.Any(x => x.IsActive),
            HasInactiveUsers = users.Any(x => !x.IsActive)
        };
    }
}