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

public sealed class StartupRepository(RiseUpDbContext db, RiseUp.Framework.Time.IClock clock) : Repository<Startup>(db), IStartupRepository
{
    private IQueryable<Startup> Visible => Set.Where(s => !s.IsDeleted && !s.IsSuspended);

    public async Task<IReadOnlyList<Startup>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(s => ids.Contains(s.Id)).ToListAsync(ct);
    public Task<Startup?> GetByOwnerAsync(int ownerAccountId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.OwnerAccountId == ownerAccountId && !s.IsDeleted, ct);
    public Task<Startup?> GetWithMetricsAsync(int id, CancellationToken ct = default) =>
        Set.Include(s => s.Metrics).FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task<bool> LegalNameExistsAsync(string normalizedLegalName, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.NormalizedLegalName == normalizedLegalName && !s.IsDeleted && s.Id != (excludeId ?? 0), ct);

    public async Task<(IReadOnlyList<Startup> Items, int Total)> SearchAsync(StartupSearch q, CancellationToken ct = default)
    {
        var query = Visible.AsNoTracking();
        query = q.VerifiedOnly
            ? query.Where(s => (s.VerificationStatus == VerificationStatus.Verified && s.VerifiedUntilUtc > clock.UtcNow))
            : query.Where(s => (s.VerificationStatus == VerificationStatus.Verified && s.VerifiedUntilUtc > clock.UtcNow) || s.VerificationStatus == VerificationStatus.Pending);
        if (!string.IsNullOrWhiteSpace(q.Text))
        {
            var t = q.Text.Trim();
            query = query.Where(s => s.Name.Contains(t) || s.Tagline.Contains(t) || s.Description.Contains(t));
        }
        if (!string.IsNullOrWhiteSpace(q.Industry)) query = query.Where(s => s.Industry == q.Industry);
        if (q.Stage is not null) query = query.Where(s => s.Stage == q.Stage);
        if (!string.IsNullOrWhiteSpace(q.Location)) query = query.Where(s => s.Location == q.Location);
        var total = await query.CountAsync(ct);
        query = q.Sort switch
        {
            "views" => query.OrderByDescending(s => s.ViewCount).ThenBy(s => s.Id),
            _ => query.OrderByDescending(s => s.CreatedAtUtc).ThenBy(s => s.Id),
        };
        return (await query.Skip(q.Skip).Take(q.Take).ToListAsync(ct), total);
    }

    public async Task<IReadOnlyList<Startup>> ListActiveVerifiedAsync(int take, CancellationToken ct = default) =>
        await Visible.AsNoTracking().Where(s => (s.VerificationStatus == VerificationStatus.Verified && s.VerifiedUntilUtc > clock.UtcNow)).OrderBy(s => s.Id).Take(take).ToListAsync(ct);
    public Task<bool> HasViewSinceAsync(int startupId, string viewerKey, DateTime sinceUtc, CancellationToken ct = default) =>
        Db.ProfileViewEvents.AnyAsync(v => v.StartupId == startupId && v.ViewerKey == viewerKey && v.ViewedAtUtc >= sinceUtc, ct);
    public async Task AddViewAsync(ProfileViewEvent view, CancellationToken ct = default) => await Db.ProfileViewEvents.AddAsync(view, ct);
}
