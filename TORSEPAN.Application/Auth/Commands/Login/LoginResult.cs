namespace TORSEPAN.Application.Auth.Commands.Login;

public sealed class LoginResult
{
    public bool Success { get; init; }

    public string Token { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = new();
}