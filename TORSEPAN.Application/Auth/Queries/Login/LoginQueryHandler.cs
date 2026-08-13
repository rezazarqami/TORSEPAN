using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Auth.Queries.Login;

public sealed class LoginQueryHandler
    : IRequestHandler<LoginQuery, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginQueryHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> Handle(
        LoginQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is inactive.");

        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .ToList();

        var refreshToken = new RefreshToken(
            user.Id,
            _jwtService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(30));

        await _refreshTokenRepository.AddAsync(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(
                user.Id,
                user.UserName,
                user.FullName,
                user.Title,
                roles),

            RefreshToken = refreshToken.Token
        };
    }
}
