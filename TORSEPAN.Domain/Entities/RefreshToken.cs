using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        string token,
        DateTime expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        Revoked = false;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string Token { get; private set; } = string.Empty;

    public DateTime ExpiresAt { get; private set; }

    public bool Revoked { get; private set; }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    public bool IsValid()
    {
        return !Revoked && !IsExpired();
    }

    public void Revoke()
    {
        Revoked = true;
    }
}