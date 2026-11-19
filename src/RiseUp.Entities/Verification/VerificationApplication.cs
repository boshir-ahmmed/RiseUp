using RiseUp.Entities.Common;

namespace RiseUp.Entities.Verification;

/// <summary>Manual verification. Every decision is made by a Verification Administrator, never AI (FR-015).</summary>
public class VerificationApplication : BaseEntity
{
    public VerificationSubject Subject { get; set; }
    public int? StartupId { get; set; }
    public int? InvestorId { get; set; }
    public VerificationStatus Status { get; set; }
    public LicenceHolderType? LicenceHolderType { get; set; }
    public string? LicenceHolderName { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public int? ReviewerAccountId { get; set; }
    public string? DecisionReason { get; set; }
    public List<VerificationDocument> Documents { get; set; } = [];
}

public enum EvidenceRetention { Available, Held, Erasing }

public class VerificationDocument : BaseEntity
{
    public int ApplicationId { get; set; }
    public string Kind { get; set; } = string.Empty; // NID, Passport, TradeLicence, OwnershipProof, Credential
    public string StorageKey { get; set; } = string.Empty;
    public ScanStatus ScanStatus { get; set; }
    public EvidenceRetention Retention { get; set; }
    public bool Erased { get; set; }
}

/// <summary>Requires both owners' consent plus manual approval (FR-034/035).</summary>
public class OwnershipTransferCase : BaseEntity
{
    public int StartupId { get; set; }
    public int FromAccountId { get; set; }
    public string IncomingOwnerName { get; set; } = string.Empty;
    public string IncomingOwnerEmail { get; set; } = string.Empty;
    public DateTime? OwnerConsentAtUtc { get; set; }
    public DateTime? IncomingConsentAtUtc { get; set; }
    public TransferStatus Status { get; set; }
    public int? ReviewerAccountId { get; set; }
    public string? DecisionReason { get; set; }
}
