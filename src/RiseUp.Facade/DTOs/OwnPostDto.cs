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

public sealed record OwnPostDto(int Id, PostCategory Category, string? LiveTitle, string? PendingTitle, ModerationState PendingState, string? ReasonCode, bool IsHidden, bool HasOpenPromotion, string? Body, string? VideoUrl, string? PosterUrl, DateTime? PublishedAtUtc);
