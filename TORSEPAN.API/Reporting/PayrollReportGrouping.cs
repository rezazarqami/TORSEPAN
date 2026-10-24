using System.Globalization;
using System.Text.RegularExpressions;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.API.Reporting;

/// <summary>Report identities use musical meaning, never legacy database IDs.</summary>
public static class PayrollReportGrouping
{
    public static string BowlTitle(Bowl bowl)
    {
        var parsed = ScaleFamilyNames.Parse(CanonicalFamily(bowl.Scale?.Name));
        var count = bowl.KnownNoteCount ?? ScaleFamilyNames.GenericNoteCount(bowl.Scale?.Name)
            ?? (bowl.BowlType == TORSEPAN.Domain.Enums.BowlType.Top ? parsed.NoteCount : null);
        var family = parsed.FamilyName;
        return string.Join(" — ", new[] { family, count.HasValue ? $"{count} نوت" : "تعداد نوت نامشخص" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public static string HandpanTitle(Handpan handpan)
    {
        var parsed = ScaleFamilyNames.Parse(CanonicalFamily(handpan.Scale?.Name));
        var top = handpan.Assembly.TopBowl.KnownNoteCount;
        var bottom = handpan.Assembly.BottomBowl.KnownNoteCount;
        var total = top.HasValue && bottom.HasValue ? top + bottom : handpan.NoteCount ?? parsed.NoteCount;
        var count = total.HasValue ? $"{total} نوت" : "تعداد نوت نامشخص";
        // Top always precedes bottom, including cases where either side is unknown.
        var split = $"\u200E({top?.ToString(CultureInfo.InvariantCulture) ?? "نامشخص"}+{bottom?.ToString(CultureInfo.InvariantCulture) ?? "نامشخص"})\u200E";
        if (!top.HasValue || !bottom.HasValue)
            split = $"(رو: {top?.ToString(CultureInfo.InvariantCulture) ?? "نامشخص"}، زیر: {bottom?.ToString(CultureInfo.InvariantCulture) ?? "نامشخص"})";
        return $"{parsed.FamilyName ?? handpan.Scale?.Name ?? "اسکیل نامشخص"} — {count} {split}";
    }

    public static string NormalizeTitle(string? value)
    {
        var text = string.Concat((value ?? "").Select(c => char.IsDigit(c)
            ? ((int)char.GetNumericValue(c)).ToString(CultureInfo.InvariantCulture) : c.ToString()));
        text = Regex.Replace(text.Replace("نت", "نوت"), @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(text)) return "تعداد نوت نامشخص";
        text = CanonicalFamily(text);
        // Canonicalize historical family/count aliases as well as current labels.
        if (!text.Contains('(') && !text.Contains("نامشخص") && !text.Contains('—'))
        {
            var parsed = ScaleFamilyNames.Parse(text);
            if (parsed.FamilyName is not null)
                return parsed.NoteCount.HasValue ? $"{parsed.FamilyName} — {parsed.NoteCount} نوت" : $"{parsed.FamilyName} — تعداد نوت نامشخص";
        }
        return text;
    }

    private static string CanonicalFamily(string? value) => Regex.Replace(value ?? "",
        @"\bD\s*Kurd\s*[-–]?\s*Cyclops\b", "D Kurd Cyclops", RegexOptions.IgnoreCase);

    public static List<PayrollLine> Merge(IEnumerable<PayrollLine> lines) => lines
        .Select(x => x with { ScaleName = x.Action is 2 or 3 or 6 or 7 ? NormalizeTitle(x.ScaleName) : x.ScaleName, IsCustom = false })
        .GroupBy(x => new { x.UserId, x.Action, x.MaterialId, Material = x.MaterialName.Trim(), x.BowlType,
            Scale = x.ScaleName.ToUpperInvariant(), x.IsExport })
        .Select(g =>
        {
            var first = g.First();
            var count = g.Sum(x => x.Count);
            var total = g.Sum(x => x.Total);
            return first with
            {
                Count = count, Total = total, Rate = count == 0 ? 0 : total / count,
                UnknownCodes = g.SelectMany(x => x.UnknownCodes ?? [])
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            };
        })
        .OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName).ThenBy(x => x.IsExport)
        .ThenBy(x => ActionOrder(x.Action)).ThenBy(x => x.BowlType)
        .ThenBy(x => x.MaterialName).ThenBy(x => x.ScaleName).ToList();

    private static int ActionOrder(int action) => action switch { 2 => 1, 3 => 2, 13 => 3, 5 => 4, 6 => 5, 7 => 6, _ => 99 };
}
