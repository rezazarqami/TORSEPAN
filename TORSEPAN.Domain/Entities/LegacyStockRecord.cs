namespace TORSEPAN.Domain.Entities;

/// <summary>Opening stock is historical attribution, never newly earned production wages.</summary>
public static class LegacyStockRecord
{
    public const string Prefix = "LEGACY_STOCK:";
    public const string UnknownBottomPrefix = "LEGACY-BOTTOM-";
    public static string DisplayDescription(string description) => description.StartsWith(Prefix + "IMPORT:", StringComparison.Ordinal)
        ? "ورود ساز قدیمی به موجودی؛ دستمزد قبلاً محاسبه شده است."
        : description.StartsWith(Prefix, StringComparison.Ordinal) ? description[Prefix.Length..].Trim() : description;
    public static string DisplayCode(string code) => code.StartsWith(UnknownBottomPrefix, StringComparison.Ordinal)
        ? "نامشخص (کاسه زیر ساز قدیمی)" : code;
}
