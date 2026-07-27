using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserDisplay;

public sealed class GetUserDisplayQueryHandler
    : IRequestHandler<GetUserDisplayQuery, UserDisplayDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserDisplayQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDisplayDto?> Handle(
        GetUserDisplayQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserDisplayDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName
        };
    }
}