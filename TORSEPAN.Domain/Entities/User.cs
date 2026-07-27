using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class User : Entity
{
    public string UserName { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public ICollection<ProductionEvent> ProductionEvents { get; private set; }
        = new List<ProductionEvent>();

    public ICollection<RefreshToken> RefreshTokens { get; private set; }
        = new List<RefreshToken>();

    private User()
    {
    }

    public User(string userName, string fullName)
    {
        UserName = userName;
        FullName = fullName;
        IsActive = true;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ChangeUserName(string userName)
    {
        UserName = userName;
    }

    public void ChangeFullName(string fullName)
    {
        FullName = fullName;
    }
}