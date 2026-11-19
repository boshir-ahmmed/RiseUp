using RiseUp.Entities.Common;

namespace RiseUp.Entities.Pitching;

/// <summary>
/// One reusable pitch package per startup (unique StartupId): videos, deck, documents and the structured ask.
/// Videos and documents are never sent to AI; only the founder's written note on each pitch is checked by Model 1.
/// </summary>
public class PitchPackage : BaseEntity, IConcurrencyAware
{
    public int StartupId { get; set; }
    /// <summary>Increments on every change so investors can see whether a package changed after a pitch was sent.</summary>
    public int PackageVersion { get; set; } = 1;
    public int? PitchVideoId { get; set; }
    public int? DemoVideoId { get; set; }
    public int? TeaserVideoId { get; set; }
    public int? PitchPosterId { get; set; }
    public int? DemoPosterId { get; set; }
    public int? TeaserPosterId { get; set; }
    public bool PublicTeaser { get; set; }
    public bool PublicDemo { get; set; }
    public bool CaptionsAvailable { get; set; }
    public int? DeckMediaId { get; set; }

    public decimal AskAmountUsd { get; set; }
    public FundingInstrument Instrument { get; set; } = FundingInstrument.Safe;
    /// <summary>Pre-money valuation (equity) or valuation cap (SAFE / note). Zero = uncapped / not set.</summary>
    public decimal ValuationUsd { get; set; }
    public int DiscountPercent { get; set; }
    public int RunwayMonths { get; set; } = 12;
    public decimal MinTicketUsd { get; set; }
    /// <summary>Newline-separated, max 5.</summary>
    public string Highlights { get; set; } = string.Empty;
    public Guid Version { get; set; }

    public List<PitchSlide> Slides { get; set; } = [];
    public List<UseOfFundsItem> UseOfFunds { get; set; } = [];
    public List<PitchTeamMember> Team { get; set; } = [];
    public List<PitchFaq> Faq { get; set; } = [];
    public List<PitchDocument> Documents { get; set; } = [];

    public IEnumerable<string> HighlightLines => Highlights.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class PitchSlide : BaseEntity
{
    public int PackageId { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class UseOfFundsItem : BaseEntity
{
    public int PackageId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Percent { get; set; }
}

public class PitchTeamMember : BaseEntity
{
    public int PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class PitchFaq : BaseEntity
{
    public int PackageId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

/// <summary>Light pitch material (financial model, one-pager…). Due-diligence files stay in the Confidential Library.</summary>
public class PitchDocument : BaseEntity
{
    public int PackageId { get; set; }
    public int MediaId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool AllowDownload { get; set; }
}

/// <summary>Private Q&amp;A inside a pitch room. Never read by AI.</summary>
public class PitchQuestion : BaseEntity
{
    public int RequestId { get; set; }
    public int AskedByAccountId { get; set; }
    public string AskerName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }
}

/// <summary>Engagement inside one pitch room (unique RequestId). Only recorded for the receiving investor.</summary>
public class PitchEngagement : BaseEntity, IConcurrencyAware
{
    public int RequestId { get; set; }
    public DateTime? OpenedAtUtc { get; set; }
    public int PitchWatchPercent { get; set; }
    public int DemoWatchPercent { get; set; }
    /// <summary>Bit i set = slide i viewed (max 20 slides).</summary>
    public long SlidesViewedMask { get; set; }
    public string DocumentsViewed { get; set; } = string.Empty;
    public Guid Version { get; set; }

    public int SlidesViewed => System.Numerics.BitOperations.PopCount((ulong)SlidesViewedMask);
    public int DocumentsViewedCount => DocumentsViewed.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
}

/// <summary>Investor-private deal pipeline (unique InvestorId+StartupId). Founders never see it.</summary>
public class ShortlistEntry : BaseEntity
{
    public int InvestorId { get; set; }
    public int StartupId { get; set; }
    public ShortlistStage Stage { get; set; }
    public string Note { get; set; } = string.Empty;
}

/// <summary>Meeting proposal inside a connected conversation. The call itself happens outside RiseUp.</summary>
public class Meeting : BaseEntity
{
    public int ConversationId { get; set; }
    public MemberKind ProposedByKind { get; set; }
    public int? ProposedByAccountId { get; set; }
    public string ProposedByName { get; set; } = string.Empty;
    public DateTime Slot1Utc { get; set; }
    public DateTime? Slot2Utc { get; set; }
    public DateTime? Slot3Utc { get; set; }
    public string Agenda { get; set; } = string.Empty;
    public MeetingStatus Status { get; set; }
    public DateTime? ConfirmedSlotUtc { get; set; }

    public IEnumerable<DateTime> Slots => new DateTime?[] { Slot1Utc, Slot2Utc, Slot3Utc }.Where(s => s is not null).Select(s => s!.Value);
}
