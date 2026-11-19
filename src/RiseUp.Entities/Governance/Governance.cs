using RiseUp.Entities.Common;

namespace RiseUp.Entities.Governance;

public class UserReport : BaseEntity
{
    public ReportKind Kind { get; set; }
    public string TargetReference { get; set; } = string.Empty;
    public int? ConversationId { get; set; }
    public int? MessageId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ReporterAccountId { get; set; }
    public ReportStatus Status { get; set; }
    public string? ResolutionNote { get; set; }
}

/// <summary>Immutable record of privileged actions (FR-160, NFR-25).</summary>
public class AdminActionAudit : BaseEntity
{
    public int? ActorAccountId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public bool IsRestrictedContentAccess { get; set; }
}

public class Notification : BaseEntity
{
    public int? RecipientAccountId { get; set; }
    public int? RecipientTeamAccountId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>Transactional outbox: commits never depend on email/SignalR availability.</summary>
public class OutboxEvent : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
}
