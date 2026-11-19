using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Governance;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;

namespace RiseUp.DataAccess.Repositories;

public sealed class PostRepository(RiseUpDbContext db) : Repository<StartupPost>(db), IPostRepository
{
    public async Task<IReadOnlyList<StartupPost>> ListLiveAsync(PostCategory? category, int take, CancellationToken ct = default)
    {
        var activeStartups = Db.Startups.Where(s => !s.IsSuspended && !s.IsDeleted).Select(s => s.Id);
        return await Set.AsNoTracking()
            .Where(p => p.LiveTitle != null && !p.IsHidden && activeStartups.Contains(p.StartupId) && (category == null || p.Category == category))
            .OrderByDescending(p => p.PublishedAtUtc).Take(take).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<StartupPost>> ListByStartupAsync(int startupId, bool liveOnly, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(p => p.StartupId == startupId && (!liveOnly || (p.LiveTitle != null && !p.IsHidden)))
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc).ToListAsync(ct);
    public Task<PostReaction?> GetReactionAsync(int postId, int accountId, CancellationToken ct = default) =>
        Db.PostReactions.FirstOrDefaultAsync(r => r.PostId == postId && r.ActorAccountId == accountId, ct);
    public async Task AddReactionAsync(PostReaction reaction, CancellationToken ct = default) => await Db.PostReactions.AddAsync(reaction, ct);
    public void RemoveReaction(PostReaction reaction) => Db.PostReactions.Remove(reaction);
    public Task<Dictionary<int, int>> CountReactionsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct = default) =>
        Db.PostReactions.Where(r => postIds.Contains(r.PostId)).GroupBy(r => r.PostId).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C, ct);
    public async Task<HashSet<int>> ReactedPostIdsAsync(int accountId, IReadOnlyCollection<int> postIds, CancellationToken ct = default) =>
        (await Db.PostReactions.Where(r => r.ActorAccountId == accountId && postIds.Contains(r.PostId)).Select(r => r.PostId).ToListAsync(ct)).ToHashSet();
    public async Task<IReadOnlyList<PostComment>> ListCommentsAsync(int postId, CancellationToken ct = default) =>
        await Db.PostComments.AsNoTracking().Where(c => c.PostId == postId && c.DeletedAtUtc == null).OrderBy(c => c.CreatedAtUtc).Take(500).ToListAsync(ct);
    public Task<Dictionary<int, int>> CountCommentsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct = default) =>
        Db.PostComments.Where(c => postIds.Contains(c.PostId) && c.DeletedAtUtc == null).GroupBy(c => c.PostId).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C, ct);
    public Task<PostComment?> GetCommentAsync(int id, CancellationToken ct = default) => Db.PostComments.FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task AddCommentAsync(PostComment comment, CancellationToken ct = default) => await Db.PostComments.AddAsync(comment, ct);
}
