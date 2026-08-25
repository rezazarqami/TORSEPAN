using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class DesignType : Entity
{
    private DesignType() { }
    public DesignType(string name) { Rename(name); IsActive = true; CreatedAt = DateTime.UtcNow; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public void Rename(string name) { Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("نام دیزاین الزامی است.") : name.Trim(); }
    public void Deactivate() => IsActive = false;
}
