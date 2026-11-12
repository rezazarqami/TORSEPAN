namespace TORSEPAN.Application.Scales;

public sealed record ScalePlaygroundNote(
    string Pitch, double X, double Y, bool Bottom = false, byte[]? Audio = null, string? AudioType = null);

public sealed record ScalePlayground(IReadOnlyList<ScalePlaygroundNote> Notes)
{
    public const int MaxNotes = 32;
    public const int MaxAudioBytes = 200_000;

    private static readonly string[] PitchClasses = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "Bb", "B"];
    public static IReadOnlyList<string> SoundBank { get; } =
        Enumerable.Range(41, 45).Select(midi => $"{PitchClasses[midi % 12]}{midi / 12 - 1}").ToArray();

    public static bool IsPitch(string? pitch)
    {
        if (pitch is null || !System.Text.RegularExpressions.Regex.IsMatch(pitch, @"\A[A-Ga-g][#b]?[2-6]\z"))
            return false;
        var semitone = char.ToUpperInvariant(pitch[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, _ => 11
        };
        if (pitch.Length == 3) semitone += pitch[1] == '#' ? 1 : -1;
        var midi = (pitch[^1] - '0' + 1) * 12 + semitone;
        return midi is >= 41 and <= 85;
    }
}

public sealed record SaveScalePlayground(ScalePlayground Playground, byte[]? TopPhoto = null, byte[]? BottomPhoto = null);

public sealed record VirtualScaleSummary(Guid Id, string Name);
public sealed record SaveVirtualScale(string Name, SaveScalePlayground Design, Guid? SourceScaleId = null);
