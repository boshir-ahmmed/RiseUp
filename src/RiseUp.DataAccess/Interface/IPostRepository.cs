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

namespace RiseUp.Entities.Abstractions;

public interface IPostRepository : IRepository<StartupPost>
{
    Task<IReadOnlyList<StartupPost>> ListLiveAsync(PostCategory? category, int take, CancellationToken ct = default);
    Task<IReadOnlyList<StartupPost>> ListByStartupAsync(int startupId, bool liveOnly, CancellationToken ct = default);
    Task<PostReaction?> GetReactionAsync(int postId, int accountId, CancellationToken ct = default);
    Task AddReactionAsync(PostReaction reaction, CancellationToken ct = default);
    void RemoveReaction(PostReaction reaction);
    Task<Dictionary<int, int>> CountReactionsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct = default);
    Task<HashSet<int>> ReactedPostIdsAsync(int accountId, IReadOnlyCollection<int> postIds, CancellationToken ct = default);
    Task<IReadOnlyList<PostComment>> ListCommentsAsync(int postId, CancellationToken ct = default);
    Task<Dictionary<int, int>> CountCommentsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct = default);
    Task<PostComment?> GetCommentAsync(int id, CancellationToken ct = default);
    Task AddCommentAsync(PostComment comment, CancellationToken ct = default);
}
