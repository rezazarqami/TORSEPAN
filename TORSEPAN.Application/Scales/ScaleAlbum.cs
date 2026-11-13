namespace TORSEPAN.Application.Scales;

public sealed record ScaleAlbumNote(string Pitch, double X, double Y);
public sealed record ScaleAlbumImage(Guid Id, string Source, IReadOnlyList<ScaleAlbumNote> Notes);
public sealed record AddScaleAlbumImage(byte[] Image);
public sealed record UpdateScaleAlbumNotes(IReadOnlyList<ScaleAlbumNote> Notes);
