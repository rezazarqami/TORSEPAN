namespace TORSEPAN.Domain.Entities;

public sealed class Material
{
    private Material()
    {
    }

    public Material(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        Name = name;
    }
}