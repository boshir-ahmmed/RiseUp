using RiseUp.Entities.Common;

namespace RiseUp.Entities.Investors;

public class InvestorProfile : BaseEntity, IConcurrencyAware
{
    public int AccountId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public string InvestorType { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    /// <summary>Comma-separated industries (normalised table is a later migration).</summary>
    public string PreferredIndustries { get; set; } = string.Empty;
    public string PreferredStages { get; set; } = string.Empty;
    /// <summary>Restricted: visible to verified startups only (FR-039/040).</summary>
    public decimal TicketMinUsd { get; set; }
    public decimal TicketMaxUsd { get; set; }
    public string Location { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; }
    public DateTime? VerifiedUntilUtc { get; set; }
    public bool ReceivePitches { get; set; } = true;
    public bool IsSuspended { get; set; }
    public bool IsDeleted { get; set; }
    public int ColorIndex { get; set; }
    public Guid Version { get; set; }

    public List<InvestorClaim> Claims { get; set; } = [];

    public bool IsActiveVerifiedAt(DateTime utcNow) => VerifiedUntilUtc > utcNow && VerificationStatus == VerificationStatus.Verified && !IsSuspended && !IsDeleted;
    public IEnumerable<string> Industries => PreferredIndustries.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public IEnumerable<FundingStage> Stages => PreferredStages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Enum.Parse<FundingStage>);
}

/// <summary>Prior-investment claim; unconfirmed claims must be labelled distinctly (FR-033).</summary>
public class InvestorClaim : BaseEntity
{
    public int InvestorId { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Round { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public string? EvidenceReference { get; set; }
}
