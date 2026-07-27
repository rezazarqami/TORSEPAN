using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserPublicCard;

public sealed class GetUserPublicCardQueryHandler
    : IRequestHandler<GetUserPublicCardQuery, UserPublicCardDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserPublicCardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPublicCardDto?> Handle(
        GetUserPublicCardQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserPublicCardDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}