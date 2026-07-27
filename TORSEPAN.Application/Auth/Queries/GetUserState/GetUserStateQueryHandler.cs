using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserState;

public sealed class GetUserStateQueryHandler
    : IRequestHandler<GetUserStateQuery, UserStateDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserStateQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserStateDto?> Handle(
        GetUserStateQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserStateDto
        {
            Id = user.Id,
            UserName = user.UserName,
            IsActive = user.IsActive
        };
    }
}