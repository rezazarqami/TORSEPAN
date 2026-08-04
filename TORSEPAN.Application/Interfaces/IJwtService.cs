namespace TORSEPAN.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(
        Guid userId,
        string userName,
        string fullName,
        IEnumerable<string> roles);

    string GenerateRefreshToken();
}