using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public sealed class Scale
{
    private Scale() { }

    public Scale(string name, ScaleUsage usage)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scale name is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        SetUsage(usage);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public ScaleUsage Usage { get; private set; } = ScaleUsage.All;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void AddUsage(ScaleUsage usage) => SetUsage(Usage | usage);
    public void RemoveUsage(ScaleUsage usage)
    {
        Usage &= ~usage;
        if (Usage == ScaleUsage.None) Deactivate();
    }
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Scale name is required.");
        Name = name.Trim();
    }
    private void SetUsage(ScaleUsage usage)
    {
        if (usage == ScaleUsage.None || (usage & ~ScaleUsage.All) != 0)
            throw new ArgumentException("A valid scale usage is required.");
        Usage = usage;
    }
    public ICollection<Handpan> Handpans { get; private set; } = new List<Handpan>();
}
