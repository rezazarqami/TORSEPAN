using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace TORSEPAN.API.Security;

// A bounded, non-interlaced PNG validator. Retain only pixel chunks, removing metadata.
public static class ProfilePng
{
    public const int MaximumBytes = 131_072;
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];
    public static byte[] Sanitize(byte[] data)
    {
        if (data.Length is < 45 or > MaximumBytes || !data.AsSpan(0, 8).SequenceEqual(Signature)) throw new InvalidDataException();
        byte[]? header = null; using var pixels = new MemoryStream(); var ended = false; var offset = 8; var afterPixels = false;
        while (offset < data.Length)
        {
            if (data.Length - offset < 12) throw new InvalidDataException();
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
            if (length > MaximumBytes || length > data.Length - offset - 12) throw new InvalidDataException();
            var count = (int)length; var type = Encoding.ASCII.GetString(data, offset + 4, 4); var chunk = data.AsSpan(offset + 8, count);
            if (Crc(data.AsSpan(offset + 4, count + 4)) != BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8 + count, 4))) throw new InvalidDataException();
            if (header is null && type != "IHDR") throw new InvalidDataException();
            switch (type)
            {
                case "IHDR":
                    if (header is not null || count != 13) throw new InvalidDataException();
                    header = chunk.ToArray(); var width = BinaryPrimitives.ReadUInt32BigEndian(header); var height = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
                    if (width is 0 or > 256 || height is 0 or > 256 || header[8] != 8 || header[9] is not (2 or 6) || header[10] != 0 || header[11] != 0 || header[12] != 0) throw new InvalidDataException();
                    break;
                case "IDAT": if (afterPixels) throw new InvalidDataException(); pixels.Write(chunk); break;
                case "IEND": if (count != 0 || pixels.Length == 0) throw new InvalidDataException(); ended = true; break;
                default:
                    if (type.Length != 4 || type.Any(c => !char.IsAsciiLetter(c)) || char.IsUpper(type[0])) throw new InvalidDataException();
                    if (pixels.Length > 0) afterPixels = true;
                    break;
            }
            offset += count + 12;
            if (ended) break;
        }
        if (!ended || offset != data.Length || header is null) throw new InvalidDataException();
        var rowLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header) * (header[9] == 6 ? 4 : 3) + 1);
        var rows = (int)BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4)); pixels.Position = 0;
        using (var decoder = new ZLibStream(pixels, CompressionMode.Decompress, leaveOpen: true))
        {
            var row = new byte[rowLength];
            for (var i = 0; i < rows; i++) { decoder.ReadExactly(row); if (row[0] > 4) throw new InvalidDataException(); }
            if (decoder.ReadByte() != -1) throw new InvalidDataException();
        }
        using var output = new MemoryStream(); output.Write(Signature); WriteChunk(output, "IHDR", header); WriteChunk(output, "IDAT", pixels.ToArray()); WriteChunk(output, "IEND", []); return output.ToArray();
    }
    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> value = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(value, (uint)data.Length); stream.Write(value);
        var bytes = Encoding.ASCII.GetBytes(type).Concat(data).ToArray(); stream.Write(bytes); BinaryPrimitives.WriteUInt32BigEndian(value, Crc(bytes)); stream.Write(value);
    }
    private static uint Crc(ReadOnlySpan<byte> data)
    {
        uint value = uint.MaxValue;
        foreach (var b in data) { value ^= b; for (var bit = 0; bit < 8; bit++) value = (value >> 1) ^ ((value & 1) == 0 ? 0 : 0xedb88320u); }
        return value ^ uint.MaxValue;
    }
}
