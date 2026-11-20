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

public sealed class PromotionRepository(RiseUpDbContext db) : Repository<Promotion>(db), IPromotionRepository
{
    public Task<Dictionary<int, int>> ActiveIdsByPostAsync(IReadOnlyCollection<int> postIds, CancellationToken ct) =>
        Set.Where(p => p.Status == PromotionStatus.Active && postIds.Contains(p.PostId)).ToDictionaryAsync(p => p.PostId, p => p.Id, ct);

    public Task RecordImpressionsAsync(IReadOnlyCollection<int> promotionIds, bool fallback, CancellationToken ct) =>
        Set.Where(p => promotionIds.Contains(p.Id)).ExecuteUpdateAsync(update => update
            .SetProperty(p => p.Impressions, p => p.Impressions + 1)
            .SetProperty(p => p.FallbackImpressions, p => p.FallbackImpressions + (fallback ? 1 : 0)), ct);

    public Task<Promotion?> GetOpenForPostAsync(int postId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.PostId == postId && p.Status != PromotionStatus.Completed, ct);
    public async Task<IReadOnlyList<Promotion>> ListByStatusAsync(PromotionStatus status, CancellationToken ct = default) =>
        await Set.Where(p => p.Status == status).ToListAsync(ct);
    public async Task<IReadOnlyList<Promotion>> ListForStartupAsync(int startupId, CancellationToken ct = default) =>
        await Set.Where(p => p.StartupId == startupId).OrderByDescending(p => p.StartedAtUtc).ToListAsync(ct);
}
