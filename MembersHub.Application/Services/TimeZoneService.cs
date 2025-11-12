namespace MembersHub.Application.Services;

/// <summary>
/// Service for handling timezone conversions between UTC and Greek time (Athens)
/// </summary>
public class TimeZoneService
{
    private readonly TimeZoneInfo _greekTimeZone;

    public TimeZoneService()
    {
        // Greece uses "GTB Standard Time" (Greece, Turkey, Bulgaria)
        try
        {
            _greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback for Linux/macOS systems
            _greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");
        }
    }

    /// <summary>
    /// Convert UTC time to Greek local time
    /// </summary>
    public DateTime ConvertToGreekTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _greekTimeZone);
    }

    /// <summary>
    /// Convert Greek local time to UTC
    /// </summary>
    public DateTime ConvertToUtc(DateTime greekDateTime)
    {
        // If already UTC, return as is
        if (greekDateTime.Kind == DateTimeKind.Utc)
        {
            return greekDateTime;
        }

        // Treat as unspecified or local Greek time
        var unspecified = DateTime.SpecifyKind(greekDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _greekTimeZone);
    }

    /// <summary>
    /// Get current Greek time
    /// </summary>
    public DateTime GetGreekNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _greekTimeZone);
    }

    /// <summary>
    /// Get current Greek date (midnight Greek time, stored as UTC)
    /// </summary>
    public DateTime GetGreekToday()
    {
        var greekNow = GetGreekNow();
        var greekDate = new DateTime(greekNow.Year, greekNow.Month, greekNow.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return ConvertToUtc(greekDate);
    }

    /// <summary>
    /// Convert a date-only value to UTC (assumes Greek timezone)
    /// </summary>
    public DateTime DateToUtc(DateTime date)
    {
        var dateOnly = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return ConvertToUtc(dateOnly);
    }

    /// <summary>
    /// Format DateTime for display in Greek time
    /// </summary>
    public string FormatGreekDateTime(DateTime utcDateTime, string format = "dd/MM/yyyy HH:mm")
    {
        var greekTime = ConvertToGreekTime(utcDateTime);
        return greekTime.ToString(format);
    }

    /// <summary>
    /// Format DateTime for display in Greek time (date only)
    /// </summary>
    public string FormatGreekDate(DateTime utcDateTime, string format = "dd/MM/yyyy")
    {
        var greekTime = ConvertToGreekTime(utcDateTime);
        return greekTime.ToString(format);
    }
}
