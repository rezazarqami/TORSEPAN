namespace TORSEPAN.API.Contracts.Auth;

public sealed class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}