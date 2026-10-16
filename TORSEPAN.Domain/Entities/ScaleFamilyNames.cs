using System.Text.RegularExpressions;

namespace TORSEPAN.Domain.Entities;

public sealed record ParsedScaleFamily(string? FamilyName, int? NoteCount, bool IsBottomLabel);

public static class ScaleFamilyNames
{
    public static ParsedScaleFamily Parse(string? value)
    {
        var name = Regex.Replace(string.Concat((value ?? "").Select(c => char.IsDigit(c) ? ((int)char.GetNumericValue(c)).ToString() : c.ToString())), @"\s+", " ").Trim();
        var bottom = name.StartsWith("زیر ") || name.EndsWith(" زیر");
        name = Regex.Replace(name, @"^(?:روی|رو|زیر)\s+|\s+(?:رو|زیر)$", "").Trim();
        if (Regex.IsMatch(name, @"^\d+\s*(نت|نوت)\s+(رو|زیر)(\s|$)"))
            return new(null, null, bottom);
        var kurd = Regex.Match(name, @"^(?:d\s*kurd|دی\s*کورد|دی\s*کرد|دیکورد|دیکرد)\s*(?<n>\d{1,2})?(?:\s+(?<v>cyclops|سایکلوپس))?$", RegexOptions.IgnoreCase);
        if (kurd.Success && kurd.Groups["n"].Success && Count(kurd.Groups["n"].Value) is null)
            return new(name, null, bottom);
        if (kurd.Success)
            return new("D Kurd" + (kurd.Groups["v"].Success ? " Cyclops" : ""), Count(kurd.Groups["n"].Value), bottom);
        var leading = Regex.Match(name, @"^(?<n>\d{1,2})\s*(?:نت|نوت|notes?)\s+(?<f>.+)$", RegexOptions.IgnoreCase);
        var trailing = Regex.Match(name, @"^(?<f>.+?)\s+(?<n>\d{1,2})(?:\s*(?:نت|نوت|notes?))?$", RegexOptions.IgnoreCase);
        var match = leading.Success ? leading : trailing;
        if (match.Success && Count(match.Groups["n"].Value) is int count && (leading.Success || count >= 5))
        {
            var family = Canonical(match.Groups["f"].Value);
            if (HasName(family)) return new(family, count, bottom);
        }
        if (Regex.IsMatch(name, @"^\d+\s*(نت|نوت)(\s*\([^)]*\))?$")) return new(null, null, bottom);
        return new(HasName(name) ? Canonical(name) : null, null, bottom);
    }

    private static int? Count(string value) => int.TryParse(value, out var n) && n is >= 1 and <= 60 ? n : null;
    private static bool HasName(string value) => !string.IsNullOrWhiteSpace(value) && value.Any(char.IsLetter) && value is not ("رو" or "روی" or "زیر") && !Regex.IsMatch(value, @"^\d");
    private static string Canonical(string value)
    {
        var key = Regex.Replace(value.Trim(), @"\s+", "").ToLowerInvariant();
        return key switch {
            "dkurd" or "دیکورد" or "دیکرد" => "D Kurd",
            "cyclops" or "سایکلوپس" or "سیکلوپس" => "Cyclops",
            "eamara" or "میآمارا" or "میهآمارا" => "E Amara",
            _ => Regex.Replace(value.Trim(), @"\s+", " ")
        };
    }

    public static int? GenericNoteCount(string? name)
    {
        var value = string.Concat((name ?? "").Select(c => char.IsDigit(c) ? ((int)char.GetNumericValue(c)).ToString() : c.ToString()));
        var m = Regex.Match(value.Trim(), @"^(?<n>\d{1,2})\s*(نت|نوت)(?:\s*(رو|زیر))?(?:\s*(\([^)]*\)|[A-G][#b]?(?:[-,،\s]+[A-G][#b]?)*))?$", RegexOptions.IgnoreCase);
        return m.Success ? Count(m.Groups["n"].Value) : null;
    }

    public static string Key(string name) => Regex.Replace(Scale.NameKey(name), @"\s+", "");
    public static bool MatchesPayrollRate(Scale? rateScale, Guid? scaleId, Guid? requestedScaleId, int? noteCount, Guid? legacyScaleId = null)
    {
        if (!requestedScaleId.HasValue) return true;
        // Existing production keeps the exact pre-normalization rate identity, including fallback behavior.
        if (legacyScaleId.HasValue) return requestedScaleId == legacyScaleId;
        return requestedScaleId == scaleId ||
            (rateScale is not null && rateScale.CanonicalScaleId.HasValue && rateScale.CanonicalScaleId == scaleId &&
                (!rateScale.LegacyNoteCount.HasValue || rateScale.LegacyNoteCount == noteCount)) ||
            (rateScale is not null && rateScale.CanonicalScaleId == null && GenericNoteCount(rateScale.Name) is int genericCount && genericCount == noteCount);
    }
}
