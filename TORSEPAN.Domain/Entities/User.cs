using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public class User : Entity
{
    public string UserName { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public ICollection<ProductionEvent> ProductionEvents { get; private set; } = new List<ProductionEvent>();

    private User()
    {
    }

    public User(string userName, string fullName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.");

        UserName = userName.Trim();
        FullName = fullName.Trim();
        IsActive = true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}