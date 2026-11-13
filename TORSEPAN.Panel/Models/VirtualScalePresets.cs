using System.Globalization;
using TORSEPAN.Application.Scales;

namespace TORSEPAN.Panel.Models;

internal sealed record VirtualScalePreset(string Name, string Top, string Bottom = "",
    string TopImage = "", string TopPositions = "", string BottomImage = "", string BottomPositions = "")
{
    internal const string ImageDirectory = "/images/workshop/yishama/";

    public ScalePhotoSources CreateDiagram() =>
        new(ImageSource(TopImage), ImageSource(BottomImage),
            new(PlaceNotes(Top, TopPositions, false).Concat(PlaceNotes(Bottom, BottomPositions, true)).ToArray()));

    private static string? ImageSource(string image) => image.Length == 0 ? null : ImageDirectory + image;

    private static IEnumerable<ScalePlaygroundNote> PlaceNotes(string pitches, string positions, bool bottom)
    {
        var notes = pitches.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (notes.Length == 0)
            yield break;

        if (positions.Length != 0)
        {
            var coordinates = positions.Split(' ');
            for (var index = 0; index < notes.Length; index++)
            {
                var point = coordinates[index].Split(',');
                yield return new(notes[index], double.Parse(point[0], CultureInfo.InvariantCulture),
                    double.Parse(point[1], CultureInfo.InvariantCulture), bottom);
            }
            yield break;
        }

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
        new("Yuki’s scale - A Minor + bottom notes", "E3 A3 B3 C4 D4 E4 F4 G4 A4 B4 C5 D5 E5", "F3 G3 F5 G5",
            TopImage: "pantam-13.jpg", TopPositions: "48.36,58.00 64.93,85.79 29.50,84.14 87.79,62.79 11.29,60.50 89.79,35.21 10.29,32.57 74.50,14.50 26.43,12.93 50.21,5.57 48.43,19.93 70.43,31.71 28.21,31.86",
            BottomImage: "bottom-shells/YUKI_SCALE_BOTTOM.jpg", BottomPositions: "83.07,67.50 22.07,69.21 77.71,17.86 22.43,22.07"),
        new("C Harmonic minor / G Hijaz 13", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5",
            TopImage: "C_HARMONIC_MINOR_TOP.jpg", TopPositions: "46.50,63.07 62.93,88.50 31.36,87.79 84.79,70.50 13.21,68.86 90.07,43.43 8.29,43.29 79.93,21.29 17.50,22.71 60.50,10.64 36.21,10.07 60.64,32.86 33.07,33.79"),
        new("C Harmonic minor / G Hijaz 19 + bottom notes", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5", "C3 D3 Eb3 F3 Ab3 B3",
            TopImage: "C_HARMONIC_MINOR_TOP.jpg", TopPositions: "46.50,63.07 62.93,88.50 31.36,87.79 84.79,70.50 13.21,68.86 90.07,43.43 8.29,43.29 79.93,21.29 17.50,22.71 60.50,10.64 36.21,10.07 60.64,32.86 33.07,33.79",
            BottomImage: "bottom-shells/C_HARMONIC_MINOR_BOTTOM.jpg", BottomPositions: "73.79,80.21 31.79,81.93 73.36,22.00 33.50,20.07 91.07,50.00 12.43,50.00"),
        new("C Harmonic minor / G Hijaz 16", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5 Ab5 B5 C6",
            TopImage: "pantam-16.jpg", TopPositions: "49.00,60.00 64.43,87.64 35.00,88.00 84.71,70.86 15.00,72.00 91.71,47.43 8.00,49.00 85.71,26.36 13.93,26.79 70.14,12.00 28.86,11.86 50.07,7.00 36.21,27.79 59.86,27.57 23.93,45.86 73.21,45.00"),
        new("C Harmonic Minor / G Hijaz 22 + bottom notes", "G3 C4 D4 Eb4 F4 G4 Ab4 B4 C5 D5 Eb5 F5 G5 Ab5 B5 C6", "C3 D3 Eb3 F3 Ab3 B3",
            TopImage: "pantam-16.jpg", TopPositions: "49.00,60.00 64.43,87.64 35.00,88.00 84.71,70.86 15.00,72.00 91.71,47.43 8.00,49.00 85.71,26.36 13.93,26.79 70.14,12.00 28.86,11.86 50.07,7.00 36.21,27.79 59.86,27.57 23.93,45.86 73.21,45.00",
            BottomImage: "bottom-shells/C_HARMONIC_MINOR_BOTTOM.jpg", BottomPositions: "73.79,80.21 31.79,81.93 73.36,22.00 33.50,20.07 91.07,50.00 12.43,50.00"),
        new("G Oxalis 11", "G3 B3 C4 D4 E4 G4 B4 C5 D5 E5 G5",
            TopImage: "pantam-11.jpg", TopPositions: "50.14,56.93 68.14,84.57 33.57,85.07 88.57,62.50 12.36,64.21 87.79,34.21 11.07,35.71 71.36,14.36 26.64,16.00 48.43,8.64 49.57,28.71"),
        new("F# Romanian Hijaz 13", "F#3 B3 C#4 D4 F4 F#4 G#4 A4 B4 C#5 D5 F5 F#5",
            TopImage: "C_HARMONIC_MINOR_TOP.jpg", TopPositions: "46.50,63.07 62.93,88.50 31.36,87.79 84.79,70.50 13.21,68.86 90.07,43.43 8.29,43.29 79.93,21.29 17.50,22.71 60.50,10.64 36.21,10.07 60.64,32.86 33.07,33.79"),
        new("F# Romanian Hijaz 20 + bottom notes", "F#3 B3 C#4 D4 F4 F#4 G#4 A4 B4 C#5 D5 F5 F#5", "B2 C#3 G#3 A3 G#5 A5 B5",
            TopImage: "C_HARMONIC_MINOR_TOP.jpg", TopPositions: "46.50,63.07 62.93,88.50 31.36,87.79 84.79,70.50 13.21,68.86 90.07,43.43 8.29,43.29 79.93,21.29 17.50,22.71 60.50,10.64 36.21,10.07 60.64,32.86 33.07,33.79",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,12.93 30.50,9.79 53.64,5.29"),
        new("F# Minor 12", "F#3 B3 C#4 D4 F#4 G#4 A4 B4 C#5 D5 F#5 G#5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("F# La Sirena 14", "F#3 A3 C#4 D#4 E4 F#4 G#4 A4 C#5 D#5 E5 F#5 G#5 A5",
            TopImage: "pantam-14.jpg", TopPositions: "49.36,56.07 51.21,88.29 20.43,75.93 79.93,77.21 8.14,50.21 91.79,52.50 15.71,23.00 87.57,28.21 30.57,9.43 74.07,12.21 52.07,3.86 51.57,15.93 32.79,30.57 70.43,31.71"),
        new("F Integral 12", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("F Integral 15", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5 Ab5 Bb5 C6",
            TopImage: "pantam-15.jpg", TopPositions: "50.07,54.93 52.71,88.50 23.43,77.93 80.29,76.71 9.00,53.36 92.36,51.21 12.86,29.00 87.57,28.21 28.86,11.86 71.07,11.57 50.36,5.71 60.86,21.71 37.21,24.07 75.79,38.00 24.36,41.14"),
        new("F Integral 19 + bottom notes", "F3 C4 Db4 Eb4 F4 G4 Ab4 C5 Db5 Eb5 F5 G5", "Db3 Eb3 Ab3 Bb3 Ab5 Bb5 C6",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        new("F Pygmy 12", "F3 G3 Ab3 C4 Eb4 F4 G4 Ab4 C5 Eb5 F5 G5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("F Pygmy 19 + bottom notes", "F3 G3 Ab3 C4 Eb4 F4 G4 Ab4 C5 Eb5 F5 G5", "Db3 Eb3 Bb3 Db4 Ab5 Bb5 C6",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        new("F Minor/ G# Major 11", "F3 Ab3 Bb3 C4 Db4 Eb4 F4 G4 Ab4 C5 F5",
            TopImage: "pantam-11.jpg", TopPositions: "50.14,56.93 68.14,84.57 33.57,85.07 88.57,62.50 12.36,64.21 87.79,34.21 11.07,35.71 71.36,14.36 26.64,16.00 48.43,8.64 49.57,28.71"),
        new("F Major 12", "F3 A3 Bb3 C4 E4 F4 G4 A4 C5 D5 E5 F5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("E La Sirena 12", "E3 G3 B3 C#4 D4 E4 F#4 G4 B4 C#5 D5 E5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("E Integral 12", "E3 B3 C4 D4 E4 F#4 G4 A4 B4 C5 D5 E5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("E Major 12", "E3 A3 B3 C#4 D#4 E4 F#4 G#4 B4 C#5 D#5 E5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("E Major 17", "E3 A3 B3 C#4 D#4 E4 F#4 G#4 B4 C#5 D#5 E5", "C#3 D#3 F#3 G#3 A4",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43",
            BottomImage: "bottom-shells/E_MAJOR_17_BOTTOM.jpg", BottomPositions: "23.86,72.14 74.36,76.43 85.57,40.29 18.43,34.79 34.79,11.57"),
        new("E Romanian Hijaz 10", "E3 A3 B3 C4 D#4 E4 F#4 G4 A4 B4",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("E Romanian Hijaz 17 + bottom notes", "E3 A3 B3 C4 D#4 E4 F#4 G4 A4 B4", "B2 C3 F#3 G3 C5 D#5 E5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        new("Kabecao Eb La sirena 16", "D#3 F#3 A#3 C4 C#4 D#4 F4 F#4 A#4", "C#3 F3 G#3 C5 C#5 D#5 F5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        new("D Major 10", "D3 G3 A3 B3 C#4 D4 E4 F#4 A4 B4",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("D Mystic 9", "D3 A3 A#3 D4 E4 F4 A4 C5 D5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("D Voyager 10", "D3 F3 A3 C4 D4 E4 F4 A4 C5 D5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("D Minor / Kurd 10", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("D Minor / Kurd 12", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5 D5 E5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("D3 Minor / Kurd 16", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5", "Bb2 C3 F3 G3 D5 E5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29",
            BottomImage: "bottom-shells/C_HARMONIC_MINOR_BOTTOM.jpg", BottomPositions: "73.79,80.21 31.79,81.93 73.36,22.00 33.50,20.07 91.07,50.00 12.43,50.00"),
        new("D Minor / Kurd 20 + bottom notes", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4 C5 D5 E5", "E3 F3 G3 F5 G5 A5 Bb5 C6",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43",
            BottomImage: "bottom-shells/D_KURD_20_BOTTOM.jpg", BottomPositions: "37.43,83.00 86.64,56.43 17.21,54.64 16.64,27.36 86.36,27.43 32.71,12.93 70.71,14.00 51.57,8.36"),
        new("D Hijaz 10", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("D Hijaz 13", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5 F#5 G5 A5",
            TopImage: "pantam-13.jpg", TopPositions: "48.36,58.00 64.93,85.79 29.50,84.14 87.79,62.79 11.29,60.50 89.79,35.21 10.29,32.57 74.50,14.50 26.43,12.93 50.21,5.57 48.43,19.93 70.43,31.71 28.21,31.86"),
        new("D Hijaz 20 + bottom notes", "D3 A3 C4 D4 Eb4 F#4 G4 A4 C5 D5 F#5 G5 A5", "A2 C3 F#3 G3 Eb5 Bb5 C6",
            TopImage: "pantam-13.jpg", TopPositions: "48.36,58.00 64.93,85.79 29.50,84.14 87.79,62.79 11.29,60.50 89.79,35.21 10.29,32.57 74.50,14.50 26.43,12.93 50.21,5.57 48.43,19.93 70.43,31.71 28.21,31.86",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        new("D Hijaz / Tarznauyn 11", "D3 A3 Bb3 C4 D4 Eb4 F#4 G4 A4 C5 D5",
            TopImage: "pantam-11.jpg", TopPositions: "50.14,56.93 68.14,84.57 33.57,85.07 88.57,62.50 12.36,64.21 87.79,34.21 11.07,35.71 71.36,14.36 26.64,16.00 48.43,8.64 49.57,28.71"),
        new("D Harmonic minor 12", "D3 A3 Bb3 C#4 D4 E4 F4 G4 A4 C#5 D5 E5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("D Celtic/Amara 10", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("D Celtic/Amara 13", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5 E5 F5 G5",
            TopImage: "pantam-13.jpg", TopPositions: "48.36,58.00 64.93,85.79 29.50,84.14 87.79,62.79 11.29,60.50 89.79,35.21 10.29,32.57 74.50,14.50 26.43,12.93 50.21,5.57 48.43,19.93 70.43,31.71 28.21,31.86"),
        new("D Celtic/Amara 20 + bottom notes", "D3 A3 C4 D4 E4 F4 G4 A4 C5 D5 E5 F5 G5", "C3 E3 F3 G3 A5 Bb5 C6",
            TopImage: "pantam-13.jpg", TopPositions: "48.36,58.00 64.93,85.79 29.50,84.14 87.79,62.79 11.29,60.50 89.79,35.21 10.29,32.57 74.50,14.50 26.43,12.93 50.21,5.57 48.43,19.93 70.43,31.71 28.21,31.86",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,12.93 30.50,9.79 53.64,5.29"),
        new("D Low Mystic 9", "D3 F3 A3 Bb3 D4 E4 F4 A4 C5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("C# Raga Desh", "C#3 F3 F#3 G#3 B3 C#4 F4 F#4 G#4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("C# Low Mystic 9", "C#3 E3 G#3 A3 C#4 D#4 E4 G#4 B4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("C# Minor/Annaziska 12", "C#3 G#3 A3 B3 C#4 D#4 E4 F#4 G#4 A4 B4 C#5",
            TopImage: "pantam-12.jpg", TopPositions: "52.21,57.64 67.14,86.93 32.71,86.36 89.14,63.93 11.21,62.36 89.79,35.21 11.21,34.86 73.86,13.71 26.43,12.93 50.21,6.64 36.07,27.14 64.93,27.43"),
        new("C Aegean 10", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("C Aegean 11", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5 F#5",
            TopImage: "pantam-11.jpg", TopPositions: "50.14,56.93 68.14,84.57 33.57,85.07 88.57,62.50 12.36,64.21 87.79,34.21 11.07,35.71 71.36,14.36 26.64,16.00 48.43,8.64 49.57,28.71"),
        new("C Aegean 20 + bottom notes", "C3 E3 G3 B3 C4 E4 F#4 G4 B4 E5 F#5", "F#3 A3 D4 A4 C5 D5 G5 A5 B5",
            TopImage: "pantam-11.jpg", TopPositions: "50.14,56.93 68.14,84.57 33.57,85.07 88.57,62.50 12.36,64.21 87.79,34.21 11.07,35.71 71.36,14.36 26.64,16.00 48.43,8.64 49.57,28.71",
            BottomImage: "bottom-shells/C_AEGEAN_19_BOTTOM.jpg", BottomPositions: "72.50,77.07 24.93,76.29 89.50,51.36 12.00,51.71 83.93,28.71 16.93,27.64 70.71,14.00 30.00,13.36 49.86,7.36"),
        new("C Golden Gate 10", "C3 E3 G3 B3 C4 D4 F#4 G4 B4 D5",
            TopImage: "pantam-10.jpg", TopPositions: "49.64,44.50 67.07,82.79 31.71,83.21 89.36,58.79 11.07,59.36 86.64,31.21 12.93,32.43 68.64,12.64 27.00,13.86 47.00,8.29"),
        new("C Hijaz 10", "C3 G3 Ab3 B3 C4 D4 Eb4 F4 G4 C5",
            TopImage: "C_HIJAZ_TOP.jpg", TopPositions: "53.43,52.36 70.36,80.86 34.93,82.50 88.79,55.57 14.64,59.07 83.71,28.71 15.79,32.00 63.43,10.43 34.79,11.93 51.07,23.36"),
        new("C Hijaz 18 + bottom notes", "C3 G3 Ab3 B3 C4 D4 Eb4 F4 G4 C5", "Eb3 F3 Ab4 B4 D5 Eb5 F5 G5",
            TopImage: "C_HIJAZ_TOP.jpg", TopPositions: "53.43,52.36 70.36,80.86 34.93,82.50 88.79,55.57 14.64,59.07 83.71,28.71 15.79,32.00 63.43,10.43 34.79,11.93 51.07,23.36",
            BottomImage: "bottom-shells/G2_MINOR_17_BOTTOM.jpg", BottomPositions: "81.71,70.29 22.79,71.43 89.50,40.64 10.64,42.43 22.36,21.64 22.79,22.36 39.64,10.79 40.14,11.14"),
        new("B2 Caesar", "B2 E3 F#3 G3 B3 D4 E4 F#4 B4", "D3 A3 C#4 G4 A4 C#5 D5 E5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/G2_MINOR_17_BOTTOM.jpg", BottomPositions: "81.71,70.29 22.79,71.43 89.50,40.64 10.64,42.43 78.79,22.36 22.36,21.64 61.43,11.14 39.64,10.79"),
        new("B2 Athena 9", "B2 F#3 G3 B3 E4 F#4 G4 B4 E5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("B2 Amara 9", "B2 F#3 A3 B3 C#4 D4 E4 F#4 A4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("B2 Hijaz 9", "B2 F#3 A3 B3 C4 D#4 E4 F#4 A4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("B2 Ursa Minor 9", "B2 F#3 G3 B3 C#4 D4 E4 F#4 G4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("B2 Onoleo 9", "B2 F#3 G3 B3 D#4 E4 F#4 G4 B4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("Bb2 Amara 9", "Bb2 F3 Ab3 Bb3 C4 Db4 Eb4 F4 Ab4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("Bb2 Amara 17 with bottom notes", "Bb2 F3 Ab3 Bb3 C4 Db4 Eb4 F4 Ab4", "Eb3 Gb3 C5 Db5 Eb5 F5 Ab5 Bb5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/G2_MINOR_17_BOTTOM.jpg", BottomPositions: "81.71,70.29 22.79,71.43 89.50,40.64 10.64,42.43 78.79,22.36 22.36,21.64 61.43,11.14 39.64,10.79"),
        new("Bb2 Aegean 9", "Bb2 D3 F3 A3 Bb3 D4 E4 F4 A4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("B2b Hijaz 9", "Bb2 F3 Ab3 Bb3 B4 D4 Eb4 F4 Ab4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("A2 Hijaz 9", "A2 E3 G3 A3 Bb3 C#4 D4 E4 G4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("A2 Hijaz 14 + bottom notes", "A2 E3 G3 A3 Bb3 C#4 D4 E4 G4", "D3 F3 F4 A4 D5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/E_MAJOR_17_BOTTOM.jpg", BottomPositions: "23.86,72.14 74.36,76.43 85.57,40.29 18.43,34.79 34.79,11.57"),
        new("A2 Amara 9", "A2 E3 G3 A3 B3 C4 D4 E4 G4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("A2 Minor 9", "A2 A3 B3 C4 D4 E4 F4 G4 A4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("G2 Minor 9", "G2 G3 A3 Bb3 C4 D4 Eb4 F4 G4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("G2 Minor 17 + bottom notes", "G2 G3 A3 Bb3 C4 D4 Eb4 F4 G4", "D3 Eb3 A4 C5 D5 Eb5 F5 G5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/G2_MINOR_17_BOTTOM.jpg", BottomPositions: "81.71,70.29 22.79,71.43 89.50,40.64 10.64,42.43 78.79,22.36 22.36,21.64 61.43,11.14 39.64,10.79"),
        new("G2 Hijaz 9", "G2 G3 Ab3 B3 C4 D4 Eb4 F4 G4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("G2 Hijaz 18 + bottom notes", "G2 G3 Ab3 B3 C4 D4 Eb4 F4 G4", "D3 F3 Ab4 B4 C5 D5 Eb5 F5 G5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/C_AEGEAN_19_BOTTOM.jpg", BottomPositions: "72.50,77.07 24.93,76.29 89.50,51.36 12.00,51.71 83.93,28.71 16.93,27.64 70.71,14.00 30.00,13.36 49.86,7.36"),
        new("F2 Pygmy 9", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("F2 Pygmy 13 + bottom notes", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4", "C3 Eb3 C5 Db5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/YUKI_SCALE_BOTTOM.jpg", BottomPositions: "83.07,67.50 22.07,69.21 77.71,17.86 22.43,22.07"),
        new("F2 Pygmy 17 + bottom notes", "F2 F3 G3 Ab3 C4 Eb4 F4 G4 Ab4", "C3 Eb3 Bb3 Db4 C5 Db5 Eb5 F5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/F2_BOTTOM.jpg", BottomPositions: "77.43,73.00 26.71,72.14 85.93,39.00 12.93,42.43 75.07,17.93 22.86,20.93 59.00,10.36 39.64,10.79"),
        new("Kabecao 'The Astronaut' 16", "F2 C3 F3 G3 Ab3 C4 Eb4 F4 G4", "Bb2 Db3 Eb3 Ab4 C5 Eb5 G5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43",
            BottomImage: "bottom-shells/F3_PYGMY_19_BOTTOM.jpg", BottomPositions: "78.64,74.86 20.64,71.64 91.07,38.43 14.64,32.57 30.50,9.79 74.79,12.93 53.64,5.29"),
        // Torsepan workshop presets with a different note set from the Yishama catalog.
        new("D Kurd 9", "D3 A3 Bb3 C4 D4 E4 F4 G4 A4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("D Celtic Minor (Amara) 9", "D3 A3 C4 D4 E4 F4 G4 A4 C5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("D Integral 9", "D3 A3 Bb3 C4 D4 E4 F4 A4 C5",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
        new("A Pygmy 9", "A2 E3 A3 B3 C4 E4 G4 A4 B4",
            TopImage: "pantam-9.jpg", TopPositions: "48.14,42.71 69.00,80.93 30.71,82.00 89.29,54.21 10.36,55.36 81.29,25.07 15.79,26.86 60.00,10.14 34.43,10.43"),
    ];
}
