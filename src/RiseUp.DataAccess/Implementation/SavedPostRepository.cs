using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Repositories;

public sealed class SavedPostRepository(RiseUpDbContext db) : ISavedPostRepository
{
    public Task<SavedPost?> GetAsync(int postId, int accountId, CancellationToken ct = default) => db.SavedPosts.FirstOrDefaultAsync(s => s.PostId == postId && s.AccountId == accountId, ct);
    public async Task AddAsync(SavedPost saved, CancellationToken ct = default) => await db.SavedPosts.AddAsync(saved, ct);
    public void Remove(SavedPost saved) => db.SavedPosts.Remove(saved);
    public async Task<HashSet<int>> SavedPostIdsAsync(int accountId, CancellationToken ct = default) =>
        (await db.SavedPosts.Where(s => s.AccountId == accountId).Select(s => s.PostId).ToListAsync(ct)).ToHashSet();
    public Task<StartupPost?> FindPostByMediaAsync(int mediaId, CancellationToken ct = default) =>
        db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.VideoMediaId == mediaId || p.PosterMediaId == mediaId, ct);
}
