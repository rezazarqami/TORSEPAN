using TORSEPAN.Application.Auth.Commands.Login;

namespace TORSEPAN.Panel.Services.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginCommand command);

    Task LogoutAsync();

    bool IsAuthenticated { get; }

    string? Token { get; }

    string? UserName { get; }

    string? FullName { get; }

    IReadOnlyList<string> Roles { get; }
}