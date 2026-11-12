namespace TORSEPAN.Domain.Entities;

public sealed class ScalePhotos
{
    private ScalePhotos() { }

    public ScalePhotos(Guid scaleId) => ScaleId = scaleId;

    public Guid ScaleId { get; private set; }
    public byte[]? Top { get; private set; }
    public byte[]? Bottom { get; private set; }
    public string? PlaygroundJson { get; private set; }

    public void SetPlayground(string json) => PlaygroundJson = json;

    public void Apply(byte[]? top, byte[]? bottom)
    {
        if (top is not null) Top = top;
        if (bottom is not null) Bottom = bottom;
    }
}
