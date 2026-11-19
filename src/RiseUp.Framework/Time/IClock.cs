namespace RiseUp.Framework.Time;

/// <summary>Injectable clock so time-based rules (14-day windows, 15-minute edits, daily quotas) are testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
