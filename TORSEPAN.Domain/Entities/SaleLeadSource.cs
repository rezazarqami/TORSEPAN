using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class SaleLeadSource : Entity
{
    private SaleLeadSource() { }
    public SaleLeadSource(string name, bool requiresReferrer = false)
    {
        Id = Guid.NewGuid();
        Name = Normalize(name);
        RequiresReferrer = requiresReferrer;
        IsActive = true;
    }
    public string Name { get; private set; } = string.Empty;
    public bool RequiresReferrer { get; private set; }
    public bool IsActive { get; private set; }
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Name is required.") : value.Trim();
}
