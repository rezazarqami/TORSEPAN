using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserIdentityLite;

public sealed class GetUserIdentityLiteQueryHandler
    : IRequestHandler<GetUserIdentityLiteQuery, UserIdentityLiteDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserIdentityLiteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserIdentityLiteDto?> Handle(
        GetUserIdentityLiteQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserIdentityLiteDto
        {
            Id = user.Id,
            UserName = user.UserName
        };
    }
}