using System.Globalization;

namespace TORSEPAN.Panel.Services;

public static class PersianDateTime
{
    private static readonly PersianCalendar Calendar = new();
    private static readonly TimeZoneInfo IranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public static string FormatDate(DateTime value) => Format(value, false);
    public static string FormatDateTime(DateTime value) => Format(value, true);
    public static string FormatTime(DateTime value) => ToIranTime(value).ToString("HH:mm");

    private static string Format(DateTime value, bool includeTime)
    {
        var iran = ToIranTime(value);
        var date = $"{Calendar.GetYear(iran):0000}/{Calendar.GetMonth(iran):00}/{Calendar.GetDayOfMonth(iran):00}";
        return includeTime ? $"{date} {iran:HH:mm}" : date;
    }

    private static DateTime ToIranTime(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, IranTimeZone);
    }
}
