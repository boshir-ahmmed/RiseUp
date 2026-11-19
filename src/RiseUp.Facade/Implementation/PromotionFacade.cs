using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Promotions;

public sealed class PromotionFacade(IPromotionRepository promotions, IPostRepository posts, IStartupRepository startups,
    IWalletRepository wallets, IPaymentRepository payments, IUnitOfWork uow, IClock clock) : IPromotionFacade
{
    public async Task<Result<int>> PurchaseAsync(Actor actor, int postId, int days, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can buy promotions.");
        var startup = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (startup is null || !startup.IsActiveVerifiedAt(clock.UtcNow)) return new Error(ErrorCodes.NotVerified, "Only verified startups can promote posts.");
        var post = await posts.GetByIdAsync(postId, ct);
        if (post is null || post.StartupId != startup.Id || !post.IsLive) return Guard.NotFound("Approved post");
        var package = (await payments.ListPromotionPackagesAsync(ct)).FirstOrDefault(p => p.Days == days);
        if (package is null) return new Error(ErrorCodes.Validation, "Choose a 1, 3 or 7-day package.");
        if (await promotions.GetOpenForPostAsync(postId, ct) is not null) return new Error(ErrorCodes.AlreadyPromoted, "This post already has a promotion.");

        await using var tx = await uow.BeginTransactionAsync(ct);
        var wallet = await wallets.GetByStartupAsync(startup.Id, ct);
        // FR-126: promotions are payable from Purchased credits only — Welcome credits are never touched.
        if (wallet is null || wallet.PurchasedBalance < package.CostPurchasedTokens)
            return new Error(ErrorCodes.InsufficientPurchasedTokens, $"Promotions need {package.CostPurchasedTokens} purchased credits. Welcome credits can't be used for promotions.");
        var now = clock.UtcNow;
        wallet.PurchasedBalance -= package.CostPurchasedTokens;
        var promotion = new Promotion
        {
            PostId = post.Id, StartupId = startup.Id, Days = days, CostTokens = package.CostPurchasedTokens,
            RemainingSeconds = (long)TimeSpan.FromDays(days).TotalSeconds, Status = PromotionStatus.Active, StartedAtUtc = now, LastTickUtc = now,
        };
        await promotions.AddAsync(promotion, ct);
        await wallets.AddLedgerAsync(new TokenLedgerEntry
        {
            StartupId = startup.Id, Description = $"Promotion · {days} day{(days > 1 ? "s" : "")}", Bucket = TokenBucket.Purchased,
            Amount = -package.CostPurchasedTokens, Reference = $"post:{post.Id}", IdempotencyKey = $"promo:{post.Id}:{now.Ticks}", ActorAccountId = actor.AccountId,
        }, ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success(promotion.Id, $"Promotion active for {days} day(s). {package.CostPurchasedTokens} purchased credits debited.");
    }

    public async Task<IReadOnlyList<PromotionDto>> ListForStartupAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return [];
        var list = await promotions.ListForStartupAsync(actor.StartupId.Value, ct);
        var result = new List<PromotionDto>();
        foreach (var p in list)
        {
            var post = await posts.GetByIdAsync(p.PostId, ct);
            result.Add(new PromotionDto(p.Id, p.PostId, post?.LiveTitle ?? "(removed)", p.Days, p.CostTokens, p.Status,
                TimeSpan.FromSeconds(p.RemainingSeconds), p.PauseReason, p.Impressions, p.Clicks, p.Engagements, p.FallbackImpressions));
        }
        return result;
    }

    public async Task<int> RunClockAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var changed = 0;
        foreach (var p in await promotions.ListByStatusAsync(PromotionStatus.Active, ct))
        {
            var startup = await startups.GetByIdAsync(p.StartupId, ct);
            if (startup is null || !startup.IsActiveVerifiedAt(clock.UtcNow))
            {
                // FR-143/148/151: lapse or suspension pauses and preserves remaining paid time.
                p.Status = PromotionStatus.Paused; p.PauseReason = startup?.IsSuspended == true ? "account suspended" : "verification lapsed";
                p.LastTickUtc = now; changed++; continue;
            }
            var elapsed = (long)Math.Max(0, (now - p.LastTickUtc).TotalSeconds);
            p.RemainingSeconds = Math.Max(0, p.RemainingSeconds - elapsed);
            p.LastTickUtc = now;
            if (p.RemainingSeconds == 0) p.Status = PromotionStatus.Completed;
            changed++;
        }
        await uow.SaveChangesAsync(ct);
        return changed;
    }

    public async Task PauseForStartupAsync(int startupId, string reason, CancellationToken ct = default)
    {
        await RunClockAsync(ct); // bank elapsed time first, so pausing never loses or gains paid time
        foreach (var p in await promotions.ListForStartupAsync(startupId, ct))
            if (p.Status == PromotionStatus.Active) { p.Status = PromotionStatus.Paused; p.PauseReason = reason; p.LastTickUtc = clock.UtcNow; }
    }

    public async Task ResumeForStartupAsync(int startupId, CancellationToken ct = default)
    {
        foreach (var p in await promotions.ListForStartupAsync(startupId, ct))
            if (p.Status == PromotionStatus.Paused && p.RemainingSeconds > 0) { p.Status = PromotionStatus.Active; p.PauseReason = null; p.LastTickUtc = clock.UtcNow; }
    }

    public async Task EndAllForStartupAsync(int startupId, CancellationToken ct = default)
    {
        foreach (var p in await promotions.ListForStartupAsync(startupId, ct))
            if (p.Status != PromotionStatus.Completed) { p.Status = PromotionStatus.Completed; p.RemainingSeconds = 0; }
    }
}
