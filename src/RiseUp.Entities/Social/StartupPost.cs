using RiseUp.Entities.Common;

namespace RiseUp.Entities.Social;

/// <summary>
/// Keeps the last AI-approved revision live while an edit waits for (or fails) a new check (FR-110–112).
/// </summary>
public class StartupPost : BaseEntity
{
    public int StartupId { get; set; }
    public PostCategory Category { get; set; }
    public string? LiveTitle { get; set; }
    public string? LiveBody { get; set; }
    public string? PendingTitle { get; set; }
    public string? PendingBody { get; set; }
    public ModerationState PendingState { get; set; }
    public string? PendingReasonCode { get; set; }
    public string? ImageStorageKey { get; set; }
    /// <summary>Optional short video (≤90 s) and its poster frame (<see cref="Media.MediaAsset"/> ids).</summary>
    public int? VideoMediaId { get; set; }
    public int? PosterMediaId { get; set; }
    public int? VideoSeconds { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public bool IsHidden { get; set; }
    public int ViewCount { get; set; }
    public int ShareCount { get; set; }

    public bool IsLive => LiveTitle is not null && !IsHidden;
}

/// <summary>Unique (PostId, ActorAccountId): one like per eligible account.</summary>
public class PostReaction : BaseEntity
{
    public int PostId { get; set; }
    public int ActorAccountId { get; set; }
}

public class PostComment : BaseEntity
{
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public int Depth { get; set; }
    public int AuthorAccountId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? DeletedAtUtc { get; set; }
}

/// <summary>Private bookmark. Unique (PostId, AccountId).</summary>
public class SavedPost : BaseEntity
{
    public int PostId { get; set; }
    public int AccountId { get; set; }
}
