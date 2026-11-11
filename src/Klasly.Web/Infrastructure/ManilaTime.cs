using System.Globalization;

namespace Klasly.Web.Infrastructure;

/// <summary>The API works in UTC; people read and type Asia/Manila time (UTC+8, no daylight saving).</summary>
public static class ManilaTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    public static DateTime Now => ToLocal(DateTime.UtcNow);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>A time typed in Manila (e.g. from a datetime-local input) with its +08:00 offset, for the API.</summary>
    public static DateTimeOffset ToOffset(DateTime local) =>
        new(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone.GetUtcOffset(local));

    /// <summary>"Nov 10, 9:00 AM"</summary>
    public static string Format(DateTime utc) => ToLocal(utc).ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>"9:00 AM"</summary>
    public static string FormatTime(DateTime utc) => ToLocal(utc).ToString("h:mm tt", CultureInfo.InvariantCulture);
}
