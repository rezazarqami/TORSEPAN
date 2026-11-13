using TORSEPAN.Application.Scales;

namespace TORSEPAN.Panel.Models;

internal sealed record VirtualScalePreset(string Name, string Top, string Bottom = "")
{
    public ScalePhotoSources CreateDiagram() =>
        new(null, null, new(PlaceNotes(Top, false).Concat(PlaceNotes(Bottom, true)).ToArray()));

    private static IEnumerable<ScalePlaygroundNote> PlaceNotes(string pitches, bool bottom)
    {
        var notes = pitches.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (notes.Length == 0)
            yield break;

        var first = 0;
        if (!bottom || notes.Length == 1)
        {
            yield return new(notes[0], 50, 49, bottom);
            first = 1;
        }

        var count = notes.Length - first;
        var outerCount = count > 12 ? 12 : count;
        for (var index = 0; index < count; index++)
        {
            var outer = index < outerCount;
            var ringIndex = outer ? index : index - outerCount;
            var ringSize = outer ? outerCount : count - outerCount;
            var offset = (ringIndex / 2 + .5) * (index % 2 == 0 ? -1 : 1);
            // Alternate sides from the player's edge so consecutive notes remain easy to find.
            var angle = Math.PI / 2 + offset * 2 * Math.PI / ringSize;
            var radius = outer ? 38 : 21;
            yield return new(notes[first + index], 50 + radius * Math.Cos(angle),
                49 + radius * Math.Sin(angle), bottom);
        }
    }
}

internal static class VirtualScalePresets
{
    // Name and pitch data from https://www.yishama.com/virtual-pantam-playground/
    // Parenthesized notes in the source are on the bottom shell.
    public static IReadOnlyList<VirtualScalePreset> All { get; } =
    [
        new("Yuki’s scale - A Minor + bottom notes", "E3 A3 B3 C4 D4 E4 F4 G4 A4 B4 C5 D5 E5", "F3 G3 F5 G5"),
        new("C Harmonic minor / G Hijaz 13", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5"),
        new("C Harmonic minor / G Hijaz 19 + bottom notes", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5", "C3 D3 Eb3 F3 Ab3 B3"),
        new("C Harmonic minor / G Hijaz 16", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5 Ab5 B5 C6"),
        new("C Harmonic Minor / G Hijaz 22 + bottom notes", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5 Ab5 B5 C6", "C3 D3 Eb3 F3 Ab3 B3"),
        new("G Oxalis 11", "G3 B3 C4 D4 E4 G4 B4 C5 D5 E5 G5"),
        new("F# Romanian Hijaz 13", "F#3 B3 C#4 D4 F4 F#4 G#4 A4 B4 C#5 D5 F5 F#5"),
        new("F# Romanian Hijaz 20 + bottom notes", "F#3 B3 C#4 D4 F4 F#4 G#4 A4 B4 C#5 D5 F5 F#5", "B2 C#3 G#3 A3 G#5 A5 B5"),
        new("F# Minor 12", "F#3 B3 C#4 D4 F#4 G#4 A4 B4 C#5 D5 F#5 G#5"),
        new("F# La Sirena 14", "F#3 A3 C#4 D#4 E4 F#4 G#4 A4 C#5 D#5 E5 F#5 G#5 A5"),
        new("F Integral 12", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5"),
        new("F Integral 15", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5 Ab5 Bb5 C6"),
        new("F Integral 19 + bottom notes", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5", "Db3 Eb3 Ab3 Bb3 Ab5 Bb5 C6"),
        new("F Pygmy 12", "F3 G3 Ab3 C4 Eb4 F4 G4 Ab4 C5 Eb5 F5 G5"),
        new("F Pygmy 19 + bottom notes", "F3 G3 Ab3 C4 Eb4 F4 G4 Ab4 C5 Eb5 F5 G5", "Db3 Eb3 Bb3 Db4 Ab5 Bb5 C6"),
        new("F Minor/ G# Major 11", "F3 Ab3 Bb3 C4 Db4 Eb4 F4 G4 Ab4 C5 F5"),
        new("F Major 12", "F3 A3 Bb3 C4 E4 F4 G4 A4 C5 D5 E5 F5"),
        new("E La Sirena 12", "E3 G3 B3 C#4 D4 E4 F#4 G4 B4 C#5 D5 E5"),
        new("E Integral 12", "E3 B3 C4 D4 E4 F#4 G4 A4 B4 C5 D5 E5"),
        new("E Major 12", "E3 A3 B3 C#4 D#4 E4 F#4 G#4 B4 C#5 D#5 E5"),
        new("E Major 17", "E3 A3 B3 C#4 D#4 E4 F#4 G#4 B4 C#5 D#5 E5", "C#3 D#3 F#3 G#3 A4"),
        new("E Romanian Hijaz 10", "E3 A3 B3 C4 D#4 E4 F#4 G4 A4 B4"),
        new("E Romanian Hijaz 17 + bottom notes", "E3 A3 B3 C4 D#4 E4 F#4 G4 A4 B4", "B2 C3 F#3 G3 C5 D#5 E5"),
        new("Kabecao Eb La sirena 16", "D#3 F#3 A#3 C4 C#4 D#4 F4 F#4 A#4", "C#3 F3 G#3 C5 C#5 D#5 F5"),
        new("D Major 10", "D3 G3 A3 B3 C#4 D4 E4 F#4 A4 B4"),
        new("D Mystic 9", "D3 A3 A#3 D4 E4 F4 A4 C5 D5"),
        new("D Voyager 10", "D3 F3 A3 C4 D4 E4 F4 A4 C5 D5"),
        new("D Minor / Kurd 10", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5"),
        new("D Minor / Kurd 12", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5 D5 E5"),
        new("D3 Minor / Kurd 16", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5", "Bb2 C3 F3 G3 D5 E5"),
        new("D Minor / Kurd 20 + bottom notes", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5 D5 E5", "E3 F3 G3 F5 G5 A5 Bb5 C6"),
        new("D Hijaz 10", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5"),
        new("D Hijaz 13", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5 F#5 G5 A5"),
        new("D Hijaz 20 + bottom notes", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5 F#5 G5 A5", "A2 C3 F#3 G3 Eb5 Bb5 C6"),
        new("D Hijaz / Tarznauyn 11", "D3 A3 Bb3 C4 D4 Eb4 F#4 G4 A4 C5 D5"),
        new("D Harmonic minor 12", "D3 A3 Bb3 C#4 D4 E4 F4 G4 A4 C#5 D5 E5"),
        new("D Celtic/Amara 10", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5"),
        new("D Celtic/Amara 13", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5 E5 F5 G5"),
        new("D Celtic/Amara 20 + bottom notes", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5 E5 F5 G5", "C3 E3 F3 G3 A5 Bb5 C6"),
        new("D Low Mystic 9", "D3 F3 A3 Bb3 D4 E4 F4 A4 C5"),
        new("C# Raga Desh", "C#3 F3 F#3 G#3 B3 C#4 F4 F#4 G#4"),
        new("C# Low Mystic 9", "C#3 E3 G#3 A3 C#4 D#4 E4 G#4 B4"),
        new("C# Minor/Annaziska 12", "C#3 G#3 A3 B3 C#4 D#4 E4 F#4 G#4 A4 B4 C#5"),
        new("C Aegean 10", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5"),
        new("C Aegean 11", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5 F#5"),
        new("C Aegean 20 + bottom notes", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5 F#5", "F#3 A3 D4 A4 C5 D5 G5 A5 B5"),
        new("C Golden Gate 10", "C3 E3 G3 B3 C4 D4 F#4 G4 B4 D5"),
        new("C Hijaz 10", "C3 G3 Ab3 B3 C4 D4 Eb4 F4 G4 C5"),
        new("C Hijaz 18 + bottom notes", "C3 G3 Ab3 B3 C4 D4 Eb4 F4 G4 C5", "Eb3 F3 Ab4 B4 D5 Eb5 F5 G5"),
        new("B2 Caesar", "B2 E3 F#3 G3 B3 D4 E4 F#4 B4", "D3 A3 C#4 G4 A4 C#5 D5 E5"),
        new("B2 Athena 9", "B2 F#3 G3 B3 E4 F#4 G4 B4 E5"),
        new("B2 Amara 9", "B2 F#3 A3 B3 C#4 D4 E4 F#4 A4"),
        new("B2 Hijaz 9", "B2 F#3 A3 B3 C4 D#4 E4 F#4 A4"),
        new("B2 Ursa Minor 9", "B2 F#3 G3 B3 C#4 D4 E4 F#4 G4"),
        new("B2 Onoleo 9", "B2 F#3 G3 B3 D#4 E4 F#4 G4 B4"),
        new("Bb2 Amara 9", "Bb2 F3 Ab3 Bb3 C4 Db4 Eb4 F4 Ab4"),
        new("Bb2 Amara 17 with bottom notes", "Bb2 F3 Ab3 Bb3 C4 Db4 Eb4 F4 Ab4", "Eb3 Gb3 C5 Db5 Eb5 F5 Ab5 Bb5"),
        new("Bb2 Aegean 9", "Bb2 D3 F3 A3 Bb3 D4 E4 F4 A4"),
        new("B2b Hijaz 9", "Bb2 F3 Ab3 Bb3 B4 D4 Eb4 F4 Ab4"),
        new("A2 Hijaz 9", "A2 E3 G3 A3 Bb3 C#4 D4 E4 G4"),
        new("A2 Hijaz 14 + bottom notes", "A2 E3 G3 A3 Bb3 C#4 D4 E4 G4", "D3 F3 F4 A4 D5"),
        new("A2 Amara 9", "A2 E3 G3 A3 B3 C4 D4 E4 G4"),
        new("A2 Minor 9", "A2 A3 B3 C4 D4 E4 F4 G4 A4"),
        new("G2 Minor 9", "G2 G3 A3 Bb3 C4 D4 Eb4 F4 G4"),
        new("G2 Minor 17 + bottom notes", "G2 G3 A3 Bb3 C4 D4 Eb4 F4 G4", "D3 Eb3 A4 C5 D5 Eb5 F5 G5"),
        new("G2 Hijaz 9", "G2 G3 Ab3 B3 C4 D4 Eb4 F4 G4"),
        new("G2 Hijaz 18 + bottom notes", "G2 G3 Ab3 B3 C4 D4 Eb4 F4 G4", "D3 F3 Ab4 B4 C5 D5 Eb5 F5 G5"),
        new("F2 Pygmy 9", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4"),
        new("F2 Pygmy 13 + bottom notes", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4", "C3 Eb3 C5 Db5"),
        new("F2 Pygmy 17 + bottom notes", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4", "C3 Eb3 Bb3 Db4 C5 Db5 Eb5 F5"),
        new("Kabecao 'The Astronaut' 16", "F2 C3 F3 G3 Ab3 C4 Eb4 F4 G4", "Bb2 Db3 Eb3 Ab4 C5 Eb5 G5"),
        // Torsepan workshop presets with a different note set from the Yishama catalog.
        new("D Kurd 9", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4"),
        new("D Celtic Minor (Amara) 9", "D3 A3 C4 D4 E4 F4 G4 A4 C5"),
        new("D Integral 9", "D3 A3 Bb3 C4 D4 E4 F4 A4 C5"),
        new("A Pygmy 9", "A2 E3 A3 B3 C4 E4 G4 A4 B4"),
    ];
}
