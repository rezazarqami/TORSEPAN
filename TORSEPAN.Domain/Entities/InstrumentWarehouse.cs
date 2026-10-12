namespace TORSEPAN.Domain.Entities;

public sealed class InstrumentWarehouse
{
    private InstrumentWarehouse() { }
    public InstrumentWarehouse(int id, string name)
    {
        if (!IsValidId(id)) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id; Rename(name);
    }
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid Version { get; private set; }
    public static bool IsValidId(int id) => id is >= 1 and <= 3;
    public void Rename(string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 60 || name.Any(char.IsControl))
            throw new ArgumentException("نام انبار باید بین ۱ تا ۶۰ حرف باشد.");
        Name = name; Version = Guid.NewGuid();
    }
}
