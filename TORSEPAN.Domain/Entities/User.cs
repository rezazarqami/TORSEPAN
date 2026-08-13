using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class User : Entity
{
    public string UserName { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    // موقتاً برای سازگاری با بخش‌های فعلی پروژه نگه داشته می‌شود.
    public string Role { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public void SetDisplayOrder(int order) => DisplayOrder = Math.Max(0, order);

    public ICollection<UserRole> UserRoles { get; private set; }
        = new List<UserRole>();

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

    public void SetPassword(string password)
    {
        PasswordHash = password;
    }

    public void SetRole(string role)
    {
        Role = role;
    }

    public void ChangePassword(string password)
    {
        PasswordHash = password;
    }

    public void ChangeRole(string role)
    {
        Role = role;
    }

    public bool VerifyPassword(string password)
    {
        return PasswordHash == password;
    }

    public bool IsInRole(string role)
    {
        return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }
}
