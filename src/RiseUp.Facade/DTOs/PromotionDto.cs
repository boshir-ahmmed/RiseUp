using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Promotions;

public sealed record PromotionDto(int Id, int PostId, string PostTitle, int Days, int Cost, PromotionStatus Status, TimeSpan Remaining,
    string? PauseReason, long Impressions, long Clicks, long Engagements, long FallbackImpressions);
