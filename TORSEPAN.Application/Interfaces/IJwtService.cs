namespace TORSEPAN.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(
        Guid userId,
        string userName,
        string fullName,
        string title,
        IEnumerable<string> roles);

    string GenerateAccessToken(Guid userId, string userName, string fullName, string title, IEnumerable<string> roles, int credentialVersion)
        => GenerateAccessToken(userId, userName, fullName, title, roles);

    string GenerateRefreshToken();
}
