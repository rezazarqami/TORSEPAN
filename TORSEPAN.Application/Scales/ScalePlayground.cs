namespace TORSEPAN.Application.Scales;

public sealed record ScalePlaygroundNote(
    string Pitch, double X, double Y, bool Bottom = false, byte[]? Audio = null, string? AudioType = null);

public sealed record ScalePlayground(IReadOnlyList<ScalePlaygroundNote> Notes)
{
    public const int MaxNotes = 32;
    public const int MaxAudioBytes = 200_000;

    public static bool IsPitch(string? pitch) =>
        pitch is not null && System.Text.RegularExpressions.Regex.IsMatch(pitch, @"\A[A-Ga-g][#b]?[1-7]\z");
}

public sealed record SaveScalePlayground(ScalePlayground Playground, byte[]? TopPhoto = null, byte[]? BottomPhoto = null);
