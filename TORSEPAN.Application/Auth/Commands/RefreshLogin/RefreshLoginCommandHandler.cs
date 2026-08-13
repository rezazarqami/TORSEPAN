using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.RefreshLogin;

public sealed class RefreshLoginCommandHandler
    : IRequestHandler<RefreshLoginCommand, RefreshLoginResponse>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshLoginCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshLoginResponse> Handle(
        RefreshLoginCommand request,
        CancellationToken cancellationToken)
    {
        var refreshToken =
            await _refreshTokens.GetByTokenAsync(request.RefreshToken);

        if (refreshToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!refreshToken.IsValid())
            throw new UnauthorizedAccessException("Refresh token expired.");

        var user = await _users.GetByIdAsync(refreshToken.UserId);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found.");

        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .ToList();

        refreshToken.Revoke();

        var newRefreshToken = new Domain.Entities.RefreshToken(
            user.Id,
            _jwtService.GenerateRefreshToken(),
            DateTime.UtcNow.AddYears(10));

        await _refreshTokens.AddAsync(newRefreshToken);

        _refreshTokens.Update(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshLoginResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(
                user.Id,
                user.UserName,
                user.FullName,
                roles),

            RefreshToken = newRefreshToken.Token
        };
    }
}
