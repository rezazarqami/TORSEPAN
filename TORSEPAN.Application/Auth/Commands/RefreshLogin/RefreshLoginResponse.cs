namespace TORSEPAN.Application.Auth.Commands.RefreshLogin;

public sealed class RefreshLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}