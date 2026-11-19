using FluentValidation;
using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Facade.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Posts;

public sealed record FeedItemDto(int Id, int StartupId, string StartupName, string StartupIndustry, string StartupLocation, bool StartupVerified, int ColorIndex,
    PostCategory Category, string Title, string Body, string? ImageKey, DateTime PublishedAtUtc, int Likes, int Comments, int Shares, bool LikedByMe, bool IsPromoted,
    string StartupTagline = "", FundingStage StartupStage = FundingStage.Seed, string? LogoUrl = null, string? VideoUrl = null, string? PosterUrl = null,
    int? VideoSeconds = null, int Views = 0, bool SavedByMe = false, IReadOnlyList<CommentDto>? LatestComments = null);
