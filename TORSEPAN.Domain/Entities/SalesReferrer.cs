using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class SalesReferrer : Entity
{
    private SalesReferrer() { }
    public SalesReferrer(string name)
    {
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.") : name.Trim();
        IsActive = true;
    }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
