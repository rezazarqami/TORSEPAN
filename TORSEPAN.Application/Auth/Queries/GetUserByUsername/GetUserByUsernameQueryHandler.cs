using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserByUsername;

public sealed class GetUserByUsernameQueryHandler
    : IRequestHandler<GetUserByUsernameQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserByUsernameQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> Handle(
        GetUserByUsernameQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        if (user is null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}