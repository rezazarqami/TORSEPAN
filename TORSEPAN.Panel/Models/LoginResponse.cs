namespace TORSEPAN.Panel.Models;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserInfo? User { get; set; }
}