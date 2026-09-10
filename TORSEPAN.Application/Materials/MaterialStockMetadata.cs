using System.Text.Json;

namespace TORSEPAN.Application.Materials;

public sealed record MaterialStockDetails(Guid MaterialId, string MaterialName, string StockKind,
    int Delta, int Balance, string? Reason);

public static class MaterialStockMetadata
{
    public const string Prefix = "MATERIAL_STOCK:";
    public static string Encode(Guid id, string name, string kind, int delta, int balance, string? reason = null)
        => Prefix + JsonSerializer.Serialize(new MaterialStockDetails(id, name, kind, delta, balance, reason));
    public static MaterialStockDetails? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        try { return JsonSerializer.Deserialize<MaterialStockDetails>(value[Prefix.Length..]); }
        catch (JsonException) { return null; }
    }
}
