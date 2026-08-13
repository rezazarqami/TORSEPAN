namespace TORSEPAN.Application;

public static class ProductionCodeNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Concat(value.Trim().Select(c => c switch
        {
            >= '۰' and <= '۹' => (char)('0' + c - '۰'),
            >= '٠' and <= '٩' => (char)('0' + c - '٠'),
            _ => c
        }));
    }
}
