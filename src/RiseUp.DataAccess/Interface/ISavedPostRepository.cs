using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface ISavedPostRepository
{
    Task<SavedPost?> GetAsync(int postId, int accountId, CancellationToken ct = default);
    Task AddAsync(SavedPost saved, CancellationToken ct = default);
    void Remove(SavedPost saved);
    Task<HashSet<int>> SavedPostIdsAsync(int accountId, CancellationToken ct = default);
    Task<StartupPost?> FindPostByMediaAsync(int mediaId, CancellationToken ct = default);
}
