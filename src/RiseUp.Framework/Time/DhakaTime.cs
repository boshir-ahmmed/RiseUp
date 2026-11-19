namespace RiseUp.Framework.Time;

/// <summary>Asia/Dhaka civil-date helper used for the investor daily invitation quota (resets 00:00 Dhaka).</summary>
public static class DhakaTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Asia/Dhaka", "Bangladesh Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Bangladesh currently observes UTC+06:00 without daylight saving.
        return TimeZoneInfo.CreateCustomTimeZone("Asia/Dhaka", TimeSpan.FromHours(6), "Asia/Dhaka", "Bangladesh Standard Time");
    }

    public static DateOnly ToDhakaDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    public static DateTime ToDhaka(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>Converts a Dhaka wall-clock time (e.g. from a datetime-local input) to UTC.</summary>
    public static DateTime FromDhaka(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);
}
