using RiseUp.Entities.Common;

namespace RiseUp.Entities.Connections;

/// <summary>
/// Unified pitch/invitation. A filtered unique index guarantees at most one Pending request per
/// startup–investor pair regardless of direction (FR-073/088, NFR-10).
/// </summary>
public class ConnectionRequest : BaseEntity, IConcurrencyAware
{
    public RequestType Type { get; set; }
    public int StartupId { get; set; }
    public int InvestorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? DecisionNote { get; set; }
    public TokenBucket? TokenBucket { get; set; }
    public string? AiDecisionVersion { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    /// <summary>Pitch package version at send time and the pitch documents the founder chose to attach.</summary>
    public int? PackageVersion { get; set; }
    public string AttachedDocumentIds { get; set; } = string.Empty;
    /// <summary>"Request more information" never pauses the 14-day window.</summary>
    public DateTime? InfoRequestedAtUtc { get; set; }
    public string? InfoRequestMessage { get; set; }
    public Guid Version { get; set; }

    public IEnumerable<int> AttachedDocuments => AttachedDocumentIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
}

/// <summary>Exactly one connection per pair (unique StartupId+InvestorId). Cannot be voluntarily closed.</summary>
public class StartupInvestorConnection : BaseEntity
{
    public int StartupId { get; set; }
    public int InvestorId { get; set; }
    public int ConversationId { get; set; }
}

public class BlockRelationship : BaseEntity
{
    public int StartupId { get; set; }
    public int InvestorId { get; set; }
    public MemberKind BlockedBy { get; set; }
}

/// <summary>Unique (InvestorId, DhakaDate). Counts successful invitations only (FR-082/087).</summary>
public class InvitationDailyCounter : BaseEntity, IConcurrencyAware
{
    public int InvestorId { get; set; }
    public DateOnly DhakaDate { get; set; }
    public int Count { get; set; }
    public Guid Version { get; set; }
}

public class PitchDraft : BaseEntity
{
    public int StartupId { get; set; }
    public int? InvestorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public ModerationState State { get; set; }
    public string? ReasonCode { get; set; }
}
