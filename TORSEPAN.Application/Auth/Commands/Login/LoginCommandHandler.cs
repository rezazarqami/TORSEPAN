using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<LoginResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        if (user is null ||
            !user.VerifyPassword(request.Password) ||
            !user.IsActive)
        {
            return new LoginResult
            {
                Success = false
            };
        }

        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .Distinct()
            .ToList();

        var token = _jwtService.GenerateAccessToken(
            user.Id,
            user.UserName,
            user.FullName,
            roles);

        return new LoginResult
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Roles = roles
        };
    }
}
