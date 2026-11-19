using RiseUp.Entities.Common;

namespace RiseUp.Entities.Promotions;

/// <summary>
/// Persists remaining ACTIVE paid duration so verification lapse / suspension pauses preserve time.
/// An AI Model 2 outage does not pause promotions (FR-139, FR-143).
/// </summary>
public class Promotion : BaseEntity, IConcurrencyAware
{
    public int PostId { get; set; }
    public int StartupId { get; set; }
    public int Days { get; set; }
    public int CostTokens { get; set; }
    public long RemainingSeconds { get; set; }
    public PromotionStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastTickUtc { get; set; }
    public string? PauseReason { get; set; }
    public long Impressions { get; set; }
    public long Clicks { get; set; }
    public long Engagements { get; set; }
    public long FallbackImpressions { get; set; }
    public Guid Version { get; set; }
}
