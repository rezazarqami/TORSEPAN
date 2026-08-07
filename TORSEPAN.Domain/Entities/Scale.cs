namespace TORSEPAN.Domain.Entities;

public sealed class Scale
{
    private Scale() { }

    public Scale(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scale name is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<Handpan> Handpans { get; private set; } = new List<Handpan>();
}
