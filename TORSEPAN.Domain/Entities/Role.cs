using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; }
        = new List<UserRole>();

    private Role()
    {
    }

    public Role(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }

    public void Rename(string displayName)
    {
        DisplayName = displayName;
    }
}