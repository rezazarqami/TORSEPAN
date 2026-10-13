using System.Globalization;
using System.Text.RegularExpressions;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls;

public static class BowlCodeSequence
{
    public static string CompactTemplate(string? value, BowlType type)
    {
        var template = ProductionCodeNormalizer.Normalize(value);
        return Regex.IsMatch(template, "^[A-Z]{2}-0{5}$")
            ? $"{template[0]}{(int)type}0000" : template;
    }

    public static bool IsValidTemplate(string? value, BowlType type) =>
        string.IsNullOrEmpty(value) || Regex.IsMatch(value, $"^[A-Z]{(int)type}0{{4}}$");

    public static BowlCodeSuggestion Suggest(Guid materialId, BowlType type, string? configuredTemplate, IEnumerable<Bowl> source)
    {
        var template = CompactTemplate(configuredTemplate, type);
        if (string.IsNullOrEmpty(template)) return new("—", "", "");
        if (!IsValidTemplate(template, type)) throw new InvalidOperationException("Invalid bowl code template.");
        var bowls = source.ToList();
        var prefix = template[..2];
        var modern = new Regex($"^{prefix}([0-9]{{4,}})$", RegexOptions.CultureInvariant);
        var legacy = new Regex($"^{template[0]}[A-Z]-([0-9]+)$", RegexOptions.CultureInvariant);
        var lastCode = "—";
        long lastNumber = 0;
        foreach (var bowl in bowls.Where(x => x.MaterialId == materialId && x.BowlType == type))
        {
            var code = ProductionCodeNormalizer.Normalize(bowl.ProductionCode);
            var match = modern.Match(code);
            if (!match.Success) match = legacy.Match(code);
            if (match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= lastNumber)
            { lastNumber = number; lastCode = code; }
        }
        var used = bowls.Select(x => ProductionCodeNormalizer.Normalize(x.ProductionCode)).ToHashSet(StringComparer.Ordinal);
        var next = checked(lastNumber + 1);
        string suggested;
        do
        {
            suggested = prefix + next.ToString("D4", CultureInfo.InvariantCulture);
            if (!used.Contains(suggested)) break;
            next = checked(next + 1);
        } while (true);
        return new(lastCode, suggested, template);
    }
}

public sealed record BowlCodeSuggestion(string LastCode, string SuggestedCode, string Template);
