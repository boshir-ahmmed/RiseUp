using RiseUp.Entities.Common;

namespace RiseUp.Entities.Profiles;

/// <summary>
/// Personal profile for a login (founder, investor, admin or team member). The display name itself stays on the
/// identity record (UserAccount.FullName / InvestorProfile.DisplayName / TeamAccount.FullName) so existing
/// authorization and audit code keeps one source of truth. Exactly one of AccountId / TeamAccountId is set.
/// </summary>
public class MemberProfile : BaseEntity, IConcurrencyAware
{
    public int? AccountId { get; set; }
    public int? TeamAccountId { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public int? LogoMediaId { get; set; }
    public int? PhotoMediaId { get; set; }
    public int? CoverMediaId { get; set; }
    public DateTime? NameChangedAtUtc { get; set; }
    public Guid Version { get; set; }
}

/// <summary>Display-name change on a verified account; reviewed against the ID on the verification file.</summary>
public class NameChangeRequest : BaseEntity
{
    public int AccountId { get; set; }
    public string FromName { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public NameChangeStatus Status { get; set; }
    public int? ReviewerAccountId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? Reason { get; set; }
}
