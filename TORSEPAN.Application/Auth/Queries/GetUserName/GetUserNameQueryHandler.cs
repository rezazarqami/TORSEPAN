using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserName;

public sealed class GetUserNameQueryHandler
    : IRequestHandler<GetUserNameQuery, UserNameDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserNameQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserNameDto?> Handle(
        GetUserNameQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserNameDto
        {
            Id = user.Id,
            UserName = user.UserName
        };
    }
}