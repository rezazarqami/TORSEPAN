namespace TORSEPAN.Domain.Entities;

public sealed class ScaleAlbumPhoto
{
    private ScaleAlbumPhoto() { }

    public ScaleAlbumPhoto(Guid scaleId, byte[] image)
    {
        Id = Guid.NewGuid();
        ScaleId = scaleId;
        Image = image;
    }

    public Guid Id { get; private set; }
    public Guid ScaleId { get; private set; }
    public byte[] Image { get; private set; } = [];
    public string? NotesJson { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public void SetNotes(string json) => NotesJson = json;
}
