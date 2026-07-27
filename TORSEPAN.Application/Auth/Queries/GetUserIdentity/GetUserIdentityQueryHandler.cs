using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdentity;

public sealed class GetUserIdentityQueryHandler
    : IRequestHandler<GetUserIdentityQuery, UserIdentityDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserIdentityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserIdentityDto?> Handle(
        GetUserIdentityQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserIdentityDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName
        };
    }
}