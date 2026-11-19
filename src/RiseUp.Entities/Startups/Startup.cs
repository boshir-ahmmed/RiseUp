using RiseUp.Entities.Common;

namespace RiseUp.Entities.Startups;

/// <summary>One startup per Founder (unique OwnerAccountId). Basic profile may be public before verification.</summary>
public class Startup : BaseEntity, IConcurrencyAware
{
    public int OwnerAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedLegalName { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public FundingStage Stage { get; set; }
    public string Location { get; set; } = string.Empty;
    public int FoundingYear { get; set; }
    public string? BusinessModel { get; set; }
    public string? Website { get; set; }
    public string Tags { get; set; } = string.Empty;
    /// <summary>Restricted: visible to verified investors only (FR-027).</summary>
    public decimal FundingGoalUsd { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public DateTime? VerifiedUntilUtc { get; set; }
    public bool WelcomeTokensGranted { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsDeleted { get; set; }
    public int ViewCount { get; set; }
    public int ColorIndex { get; set; }
    /// <summary>Public branding images (<see cref="Media.MediaAsset"/> ids).</summary>
    public int? LogoMediaId { get; set; }
    public int? CoverMediaId { get; set; }
    public Guid Version { get; set; }

    public List<StartupMetric> Metrics { get; set; } = [];

    public bool IsActiveVerifiedAt(DateTime utcNow) => VerifiedUntilUtc > utcNow && VerificationStatus == VerificationStatus.Verified && !IsSuspended && !IsDeleted;
}

/// <summary>Founder-reported business metric, restricted to verified investors.</summary>
public class StartupMetric : BaseEntity
{
    public int StartupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ProfileViewEvent : BaseEntity
{
    public int StartupId { get; set; }
    public string ViewerKey { get; set; } = string.Empty; // account id or anonymous session hash
    public DateTime ViewedAtUtc { get; set; }
}

public class TeamAccount : BaseEntity
{
    public int StartupId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    /// <summary>One-way hash only. Plaintext is shown once to the owner and never stored (FR-046).</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public TeamAccountStatus Status { get; set; }
    /// <summary>Conversation ids held before deactivation, used for "restore previous groups" (FR-054).</summary>
    public string PreviousConversationIds { get; set; } = string.Empty;
}

public class PrivateDocument : BaseEntity
{
    public int StartupId { get; set; }
    public DocumentKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public ScanStatus ScanStatus { get; set; }
}

/// <summary>Explicit per-investor grant, separate from chat membership (FR-104/105).</summary>
public class DocumentAccessGrant : BaseEntity
{
    public int DocumentId { get; set; }
    public int InvestorId { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
