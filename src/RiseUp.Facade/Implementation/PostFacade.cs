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


/// <summary>
/// Posts: AI Model 1 approves/rejects every new or edited post; the last approved revision stays live while an edit
/// is rejected or the model is down (fail closed). Comments/replies are NEVER sent to AI — they are report-moderated (FR-107).
/// </summary>
public sealed class PostFacade(IPostRepository posts, IStartupRepository startups, IInvestorRepository investors, IPromotionRepository promotions,
    MatchRanker ranker, IContentSafetyModel model1, IUnitOfWork uow, IClock clock, IValidator<SubmitPostRequest> validator,
    ILogger<PostFacade> logger, ISavedPostRepository saved, IProfileRepository profiles, MediaIntake intake, NameDirectory names, IMediaRepository media) : IPostFacade
{
    private async Task<bool> CanInteractAsync(Actor actor, CancellationToken ct)
    {
        if (actor.IsFounder && actor.StartupId is int sid) return (await startups.GetByIdAsync(sid, ct))?.IsActiveVerifiedAt(clock.UtcNow) == true;
        if (actor.IsInvestor) return (await investors.GetByIdAsync(actor.InvestorId!.Value, ct))?.IsActiveVerifiedAt(clock.UtcNow) == true;
        return false;   // visitors, team members and admins cannot react (FR-113–115)
    }

    public async Task<bool> IsPublishedByOwnerAsync(Actor actor, int id, CancellationToken ct = default) =>
        actor.IsFounder && actor.StartupId is int sid && await posts.GetByIdAsync(id, ct) is { LiveTitle: not null } post && post.StartupId == sid;

    public async Task<Result<int>> SaveDraftAsync(Actor actor, SubmitPostRequest request, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is not int sid ||
            (await startups.GetByIdAsync(sid, ct))?.IsActiveVerifiedAt(clock.UtcNow) != true)
            return Guard.Forbidden("A verified startup owner can save drafts.");
        if (!Enum.IsDefined(request.Category) || request.Title.Length > BusinessRules.PostTitleMax || request.Body.Length > BusinessRules.PostBodyMax)
            return new Error(ErrorCodes.Validation, "Check the category and text lengths.");
        StartupPost post;
        if (request.PostId is int id)
        {
            var existing = await posts.GetByIdAsync(id, ct);
            if (existing is null || existing.StartupId != sid || existing.LiveTitle is not null ||
                (existing.Category == PostCategory.Reel) != (request.Category == PostCategory.Reel)) return Guard.Forbidden();
            post = existing;
        }
        else
        {
            post = new StartupPost { StartupId = sid };
            await posts.AddAsync(post, ct);
        }
        post.Category = request.Category;
        post.PendingTitle = request.Title.Trim();
        post.PendingBody = request.Body.Trim();
        post.PendingState = ModerationState.None;
        post.PendingReasonCode = null;
        await uow.SaveChangesAsync(ct);
        return Result.Success(post.Id, "Draft saved.");
    }

    public async Task<Result<int>> SubmitAsync(Actor actor, SubmitPostRequest request, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can publish posts.");
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return invalid;
        var startup = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (startup is null || !startup.IsActiveVerifiedAt(clock.UtcNow)) return new Error(ErrorCodes.NotVerified, "Only verified startups can publish posts.");
        StartupPost post;
        if (request.PostId is int id)
        {
            var existing = await posts.GetByIdAsync(id, ct);
            if (existing is null || existing.StartupId != startup.Id) return Guard.Forbidden();
            if ((existing.Category == PostCategory.Reel) != (request.Category == PostCategory.Reel)) return Guard.Forbidden();
            post = existing;
        }
        else if (request.Category == PostCategory.Reel) return new Error(ErrorCodes.InvalidMedia, "Add a reel video first.");
        else { post = new StartupPost { StartupId = startup.Id, Category = request.Category }; await posts.AddAsync(post, ct); }
        if (request.Category == PostCategory.Reel && (post.VideoMediaId is not int videoId ||
            (await media.GetByIdAsync(videoId, ct))?.ScanStatus != ScanStatus.Clean))
            return new Error(ErrorCodes.InvalidMedia, "A reel needs a video that has passed its file scan. Check the upload before publishing.");
        if (post.LiveTitle is null && request.Category != PostCategory.Reel && (post.VideoMediaId ?? post.PosterMediaId) is int attachment &&
            (await media.GetByIdAsync(attachment, ct))?.ScanStatus != ScanStatus.Clean)
            return new Error(ErrorCodes.InvalidMedia, "Your attachment needs a clean file scan before publication.");
        post.PendingTitle = request.Title.Trim(); post.PendingBody = request.Body.Trim(); post.PendingState = ModerationState.PendingCheck;
        if (request.ImageStorageKey is not null) post.ImageStorageKey = request.ImageStorageKey;

        ContentSafetyResult safety;
        try { safety = await model1.CheckAsync("post", $"{post.PendingTitle}\n{post.PendingBody}", ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("AI Model 1 failed for post: {Error}", ex.GetType().Name);
            safety = new ContentSafetyResult(SafetyOutcome.Unavailable, null, "unknown");
        }
        if (safety.Outcome == SafetyOutcome.Unavailable)
        {
            post.PendingState = ModerationState.AiUnavailable;
            await uow.SaveChangesAsync(ct);
            return new Error(ErrorCodes.AiTemporarilyUnavailable, "Safety check unavailable. Saved as a draft and not published." + (post.LiveTitle is null ? "" : " The last approved version stays live."));
        }
        if (safety.Outcome == SafetyOutcome.Reject)
        {
            post.PendingState = ModerationState.Rejected; post.PendingReasonCode = safety.ReasonCode;
            await uow.SaveChangesAsync(ct);
            return new Error(ErrorCodes.ContentRejected, $"Rejected by the safety check ({safety.ReasonCode}). Edit and resubmit." + (post.LiveTitle is null ? "" : " The last approved version stays live."));
        }
        post.Category = request.Category;
        post.LiveTitle = post.PendingTitle; post.LiveBody = post.PendingBody; post.PendingTitle = null; post.PendingBody = null;
        post.PendingState = ModerationState.Approved; post.PendingReasonCode = null; post.PublishedAtUtc ??= clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success(post.Id, "Approved by the safety check and published.");
    }

    private async Task<IReadOnlyList<FeedItemDto>> MapAsync(Actor actor, IReadOnlyList<StartupPost> list, HashSet<int> promoted, CancellationToken ct)
    {
        var ids = list.Select(p => p.Id).ToList();
        var likes = await posts.CountReactionsAsync(ids, ct);
        var comments = await posts.CountCommentsAsync(ids, ct);
        var mine = actor.AccountId is int aid ? await posts.ReactedPostIdsAsync(aid, ids, ct) : [];
        var owners = (await startups.GetManyAsync(list.Select(p => p.StartupId).Distinct().ToList(), ct)).ToDictionary(s => s.Id);
        var savedIds = actor.AccountId is int said && (actor.IsFounder || actor.IsInvestor) ? await saved.SavedPostIdsAsync(said, ct) : [];
        var latest = new Dictionary<int, IReadOnlyList<CommentDto>>();
        foreach (var p in list.Where(p => comments.GetValueOrDefault(p.Id) > 0).Take(20))
            latest[p.Id] = (await CommentsAsync(actor, p.Id, ct)).Where(c => c.Depth == 1).TakeLast(2).ToList();
        return list.Where(p => owners.ContainsKey(p.StartupId)).Select(p =>
        {
            var s = owners[p.StartupId];
            return new FeedItemDto(p.Id, s.Id, s.Name, s.Industry, s.Location, s.IsActiveVerifiedAt(clock.UtcNow), s.ColorIndex, p.Category,
                p.LiveTitle!, p.LiveBody!, p.ImageStorageKey, p.PublishedAtUtc ?? p.CreatedAtUtc, likes.GetValueOrDefault(p.Id), comments.GetValueOrDefault(p.Id),
                p.ShareCount, mine.Contains(p.Id), promoted.Contains(p.Id), s.Tagline, s.Stage, s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId),
                p.VideoMediaId is null ? null : UrlRewrite.Media(p.VideoMediaId), p.PosterMediaId is null ? null : UrlRewrite.Media(p.PosterMediaId), p.VideoSeconds,
                p.ViewCount, savedIds.Contains(p.Id), latest.GetValueOrDefault(p.Id) ?? []);
        }).ToList();
    }

    /// <summary>"For you": promoted first (always labelled), then engagement with recency decay, plus investor relevance.</summary>
    public async Task<IReadOnlyList<FeedItemDto>> GetFeedAsync(Actor actor, string tab, PostCategory? category, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 50);
        var live = await posts.ListLiveAsync(category, 200, tab is not ("latest" or "saved"), ct);
        if (tab == "saved")
        {
            if (actor.AccountId is not int me2) return [];
            var mineSaved = await saved.SavedPostIdsAsync(me2, ct);
            return (await MapAsync(actor, live.Where(p => mineSaved.Contains(p.Id)).ToList(), [], ct)).OrderByDescending(i => i.PublishedAtUtc).Take(take).ToList();
        }
        var activePromos = await promotions.ActiveIdsByPostAsync(live.Select(p => p.Id).ToList(), ct);
        var items = await MapAsync(actor, live, activePromos.Keys.ToHashSet(), ct);
        var usedFallback = false;
        IEnumerable<FeedItemDto> ordered;
        if (tab == "latest") ordered = items.OrderByDescending(i => i.PublishedAtUtc);
        else
        {
            var now = clock.UtcNow;
            Dictionary<int, int> relevance = [];
            if (actor.IsInvestor && await investors.GetByIdAsync(actor.InvestorId!.Value, ct) is { } me)
            {
                var includeGoal = me.IsActiveVerifiedAt(clock.UtcNow);
                var owners = await startups.GetManyAsync(items.Select(i => i.StartupId).Distinct().ToList(), ct);
                var pool = owners.Select(s => MatchingFacade.ToProfile(s, includeGoal)).ToList();
                var ranked = await ranker.RankAsync(MatchingFacade.ToProfile(me, true), pool, ct);
                usedFallback = ranked.Fallback;
                relevance = ranked.Scores.ToDictionary(s => s.CandidateId, s => s.Score);
            }
            ordered = items.OrderByDescending(i => i.IsPromoted)
                .ThenByDescending(i => (i.Likes * 3 + i.Comments * 4 + i.Shares * 2) / (1 + (now - i.PublishedAtUtc).TotalDays * 0.35) + relevance.GetValueOrDefault(i.StartupId) * 0.8);
        }
        var page = ordered.Take(take).ToList();
        var shown = page.Where(p => p.IsPromoted).Select(p => activePromos[p.Id]).ToList();
        if (shown.Count > 0) await promotions.RecordImpressionsAsync(shown, usedFallback, ct);
        return page;
    }

    public async Task<Result<PostDetailDto>> GetDetailAsync(Actor actor, int postId, CancellationToken ct = default)
    {
        var p = await posts.GetByIdAsync(postId, ct);
        if (p is null || !p.IsLive) return Guard.NotFound("Post");
        var s = await startups.GetByIdAsync(p.StartupId, ct);
        if (s is null || s.IsSuspended || s.IsDeleted) return Guard.NotFound("Post");
        p.ViewCount++;
        await uow.SaveChangesAsync(ct);
        var promo = await promotions.GetOpenForPostAsync(p.Id, ct);
        var item = (await MapAsync(actor, [p], promo?.Status == PromotionStatus.Active ? [p.Id] : [], ct)).Single();
        return Result.Success(new PostDetailDto(item, await CommentsAsync(actor, p.Id, ct), p.ViewCount, await CanInteractAsync(actor, ct)));
    }

    private async Task<IReadOnlyList<CommentDto>> CommentsAsync(Actor actor, int postId, CancellationToken ct)
    {
        var list = await posts.ListCommentsAsync(postId, ct);
        var photos = await profiles.PhotoIdsForAccountsAsync(list.Select(c => c.AuthorAccountId).Distinct().ToList(), ct);
        return list.Select(c => new CommentDto(c.Id, c.ParentCommentId, c.Depth, c.AuthorName, c.Body, c.CreatedAtUtc, c.AuthorAccountId == actor.AccountId,
            photos.TryGetValue(c.AuthorAccountId, out var ph) ? UrlRewrite.Media(ph) : null)).ToList();
    }

    public async Task<IReadOnlyList<FeedItemDto>> ListForStartupAsync(Actor actor, int startupId, CancellationToken ct = default) =>
        await MapAsync(actor, await posts.ListByStartupAsync(startupId, true, ct), [], ct);

    public async Task<IReadOnlyList<OwnPostDto>> ListOwnAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return [];
        var result = new List<OwnPostDto>();
        foreach (var p in await posts.ListByStartupAsync(actor.StartupId.Value, false, ct))
            result.Add(new OwnPostDto(p.Id, p.Category, p.LiveTitle, p.PendingTitle, p.PendingState, p.PendingReasonCode, p.IsHidden, await promotions.GetOpenForPostAsync(p.Id, ct) is not null, p.PendingBody ?? p.LiveBody,
                p.VideoMediaId is int video ? UrlRewrite.Media(video) : null, p.PosterMediaId is int poster ? UrlRewrite.Media(poster) : null, p.PublishedAtUtc));
        return result;
    }

    public async Task<Result<bool>> ToggleLikeAsync(Actor actor, int postId, CancellationToken ct = default)
    {
        if (!await CanInteractAsync(actor, ct)) return Guard.Forbidden("Only signed-in verified founders and investors can like posts.");
        var p = await posts.GetByIdAsync(postId, ct);
        if (p is null || !p.IsLive) return Guard.NotFound("Post");
        var existing = await posts.GetReactionAsync(postId, actor.AccountId!.Value, ct);
        if (existing is null)
        {
            await posts.AddReactionAsync(new PostReaction { PostId = postId, ActorAccountId = actor.AccountId.Value }, ct);
            if (await promotions.GetOpenForPostAsync(postId, ct) is { Status: PromotionStatus.Active } promo) promo.Engagements++;
        }
        else posts.RemoveReaction(existing);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Success(true); }   // unique (post, account): double-click is harmless
        return Result.Success(existing is null);
    }

    public async Task<Result<int>> CommentAsync(Actor actor, int postId, int? parentCommentId, string body, CancellationToken ct = default)
    {
        if (!await CanInteractAsync(actor, ct)) return Guard.Forbidden("Only signed-in verified founders and investors can comment.");
        if (string.IsNullOrWhiteSpace(body) || body.Length > BusinessRules.CommentMax) return new Error(ErrorCodes.Validation, "Comments must be 1–1,000 characters.");
        var p = await posts.GetByIdAsync(postId, ct);
        if (p is null || !p.IsLive) return Guard.NotFound("Post");
        var depth = 1;
        if (parentCommentId is int pid)
        {
            var parent = await posts.GetCommentAsync(pid, ct);
            if (parent is null || parent.PostId != postId) return Guard.NotFound("Comment");
            if (parent.Depth >= BusinessRules.MaxCommentDepth) return new Error(ErrorCodes.Validation, "Replies are limited to three levels.");
            depth = parent.Depth + 1;
        }
        // Deliberately NO call to the content-safety model here (FR-107, acceptance test 25).
        var comment = new PostComment { PostId = postId, ParentCommentId = parentCommentId, Depth = depth, AuthorAccountId = actor.AccountId!.Value, AuthorName = await names.CurrentNameAsync(actor, ct), Body = body.Trim() };
        await posts.AddCommentAsync(comment, ct);
        if (await promotions.GetOpenForPostAsync(postId, ct) is { Status: PromotionStatus.Active } promo) promo.Engagements++;
        await uow.SaveChangesAsync(ct);
        return Result.Success(comment.Id);
    }

    public async Task<Result> DeleteCommentAsync(Actor actor, int commentId, CancellationToken ct = default)
    {
        var c = await posts.GetCommentAsync(commentId, ct);
        if (c is null) return Result.Failure(Guard.NotFound("Comment"));
        if (c.AuthorAccountId != actor.AccountId && !actor.IsAdmin(AdminRole.Moderator)) return Result.Failure(Guard.Forbidden("You can only remove your own comments."));
        c.DeletedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Comment removed.");
    }

    public async Task<Result<string>> ShareAsync(Actor actor, int postId, CancellationToken ct = default)
    {
        if (!await CanInteractAsync(actor, ct)) return Guard.Forbidden("Only verified founders and investors can share posts.");
        var p = await posts.GetByIdAsync(postId, ct);
        if (p is null || !p.IsLive) return Guard.NotFound("Post");
        p.ShareCount++;
        if (await promotions.GetOpenForPostAsync(postId, ct) is { Status: PromotionStatus.Active } promo) promo.Clicks++;
        await uow.SaveChangesAsync(ct);
        return Result.Success<string>($"/posts/{p.Id}");
    }

    public async Task<Result<bool>> ToggleSaveAsync(Actor actor, int postId, CancellationToken ct = default)
    {
        if (!(actor.IsFounder || actor.IsInvestor)) return Guard.Forbidden("Sign in as a founder or investor to save posts.");
        var p = await posts.GetByIdAsync(postId, ct);
        if (p is null || !p.IsLive) return Guard.NotFound("Post");
        var existing = await saved.GetAsync(postId, actor.AccountId!.Value, ct);
        if (existing is null) await saved.AddAsync(new SavedPost { PostId = postId, AccountId = actor.AccountId.Value }, ct); else saved.Remove(existing);
        try { await uow.SaveChangesAsync(ct); } catch (PersistenceConflictException) { return Result.Success(true); }
        return Result.Success(existing is null, existing is null ? "Saved. Find it under Saved in your feed." : "Removed from saved.");
    }

    public Task<Result> AttachImageAsync(Actor actor, int postId, UploadedFile file, CancellationToken ct = default) =>
        AttachAsync(actor, postId, MediaKind.PostImage, file, null, ct);

    public Task<Result> AttachVideoAsync(Actor actor, int postId, UploadedFile video, UploadedFile? poster, CancellationToken ct = default) =>
        AttachAsync(actor, postId, MediaKind.PostVideo, video, poster, ct);

    private async Task<Result> AttachAsync(Actor actor, int postId, MediaKind kind, UploadedFile file, UploadedFile? poster, CancellationToken ct)
    {
        if (!actor.IsFounder || actor.StartupId is not int sid) return Result.Failure(Guard.Forbidden());
        if ((await startups.GetByIdAsync(sid, ct))?.IsActiveVerifiedAt(clock.UtcNow) != true)
            return Result.Failure(ErrorCodes.NotVerified, "Only verified startups can publish media.");
        var post = await posts.GetByIdAsync(postId, ct);
        if (post is null || post.StartupId != sid) return Result.Failure(Guard.Forbidden());
        var image = kind == MediaKind.PostImage;
        if ((image && (post.LiveTitle is not null || post.Category == PostCategory.Reel)) ||
            (post.Category == PostCategory.Reel && post.LiveTitle is not null))
            return Result.Failure(ErrorCodes.Validation, "Create a new draft to share this media.");

        var accepted = await intake.AcceptAsync(kind, file, sid, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        var cover = poster is null ? null : await intake.AcceptAsync(MediaKind.VideoPoster, poster, sid, actor.AccountId, null, ct);
        if (cover is { IsFailure: true }) return cover;

        var oldVideo = post.VideoMediaId;
        var oldPoster = post.PosterMediaId;
        await using (var tx = await uow.BeginTransactionAsync(ct))
        {
            await uow.SaveChangesAsync(ct);
            post.VideoMediaId = image ? null : accepted.Value!.Id;
            post.VideoSeconds = image ? null : file.DurationSeconds;
            post.PosterMediaId = image ? accepted.Value!.Id : cover?.Value?.Id;
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        // Old files are retired only after the replacement is durable.
        await intake.DiscardAsync(oldVideo, clock.UtcNow, ct);
        await intake.DiscardAsync(oldPoster, clock.UtcNow, ct);
        if (oldVideo.HasValue || oldPoster.HasValue) await uow.SaveChangesAsync(ct);
        return Result.Success(image ? "Image added." : "Video added.");
    }
}
