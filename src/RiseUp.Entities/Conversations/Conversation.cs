using RiseUp.Entities.Common;

namespace RiseUp.Entities.Conversations;

/// <summary>One private group per startup–investor pair (unique StartupId+InvestorId).</summary>
public class Conversation : BaseEntity
{
    public int StartupId { get; set; }
    public int InvestorId { get; set; }
    public List<ConversationMember> Members { get; set; } = [];
}

/// <summary>Membership history. Access is derived from rows where RemovedAtUtc is null.</summary>
public class ConversationMember : BaseEntity
{
    public int ConversationId { get; set; }
    public MemberKind Kind { get; set; }
    public int? AccountId { get; set; }
    public int? TeamAccountId { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
    public bool Muted { get; set; }
    public bool Archived { get; set; }
}

public class Message : BaseEntity
{
    public int ConversationId { get; set; }
    public MemberKind AuthorKind { get; set; }
    public int? AuthorAccountId { get; set; }
    public int? AuthorTeamAccountId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
