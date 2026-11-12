namespace TORSEPAN.Domain.Entities;

public sealed class VirtualScaleDesign
{
    private VirtualScaleDesign() { }
    public VirtualScaleDesign(Guid id) => Id = id;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string NotesJson { get; private set; } = "";
    public byte[]? Top { get; private set; }
    public byte[]? Bottom { get; private set; }

    public void Update(string name, string notesJson, byte[]? top, byte[]? bottom)
    {
        Name = name;
        NotesJson = notesJson;
        if (top is not null) Top = top;
        if (bottom is not null) Bottom = bottom;
    }
}
