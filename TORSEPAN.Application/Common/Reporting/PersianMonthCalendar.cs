using System.Globalization;

namespace TORSEPAN.Application.Common.Reporting;

public sealed record PersianMonthPeriod(int Year, int Month, DateTime LocalStart, DateTime LocalEnd)
{
    public string Label => $"{Year}/{Month:00}";
    public DateTime UtcStart => ToUtc(LocalStart);
    public DateTime UtcEnd => ToUtc(LocalEnd);

    private static DateTime ToUtc(DateTime local) =>
        DateTime.SpecifyKind(local.AddHours(-3.5), DateTimeKind.Utc);
}

public static class PersianMonthCalendar
{
    public static IReadOnlyList<PersianMonthPeriod> LastMonths(DateTime localReference, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var calendar = new PersianCalendar();
        var currentYear = calendar.GetYear(localReference);
        var currentMonth = calendar.GetMonth(localReference);

        return Enumerable.Range(1 - count, count)
            .Select(offset => CreatePeriod(calendar, currentYear, currentMonth, offset))
            .ToList();
    }

    private static PersianMonthPeriod CreatePeriod(PersianCalendar calendar, int year, int month, int offset)
    {
        var (targetYear, targetMonth) = Shift(year, month, offset);
        var (nextYear, nextMonth) = Shift(targetYear, targetMonth, 1);
        var start = calendar.ToDateTime(targetYear, targetMonth, 1, 0, 0, 0, 0);
        var end = calendar.ToDateTime(nextYear, nextMonth, 1, 0, 0, 0, 0);
        return new PersianMonthPeriod(targetYear, targetMonth, start, end);
    }

    private static (int Year, int Month) Shift(int year, int month, int offset)
    {
        var absoluteMonth = year * 12 + month - 1 + offset;
        return (absoluteMonth / 12, absoluteMonth % 12 + 1);
    }
}
