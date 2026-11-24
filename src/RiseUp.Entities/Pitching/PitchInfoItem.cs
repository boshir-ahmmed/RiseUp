using RiseUp.Entities.Common;
using RiseUp.Entities.Media;

namespace RiseUp.Entities.Pitching;

public sealed class PitchInfoItem : BaseEntity
{
    public int RequestId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? ReplyText { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }
    public int? ReplyMediaId { get; set; }
    public MediaAsset? ReplyMedia { get; set; }
}
