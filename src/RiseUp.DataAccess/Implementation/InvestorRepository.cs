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

public sealed class InvestorRepository(RiseUpDbContext db, RiseUp.Framework.Time.IClock clock) : Repository<InvestorProfile>(db), IInvestorRepository
{
    public async Task<IReadOnlyList<InvestorProfile>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
    public Task<InvestorProfile?> GetByAccountAsync(int accountId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(i => i.AccountId == accountId && !i.IsDeleted, ct);
    public Task<InvestorProfile?> GetWithClaimsAsync(int id, CancellationToken ct = default) =>
        Set.Include(i => i.Claims).FirstOrDefaultAsync(i => i.Id == id, ct);
    public Task<IReadOnlyList<InvestorProfile>> ListActiveVerifiedAsync(int take, CancellationToken ct = default) =>
        SearchActiveVerifiedAsync(null, take, ct);

    public async Task<IReadOnlyList<InvestorProfile>> SearchActiveVerifiedAsync(string? query, int take, CancellationToken ct)
    {
        var visible = Set.AsNoTracking().Where(i => i.VerificationStatus == VerificationStatus.Verified
            && i.VerifiedUntilUtc > clock.UtcNow && !i.IsSuspended && !i.IsDeleted);
        if (string.IsNullOrWhiteSpace(query))
            return await visible.OrderBy(i => i.Id).Take(take).ToListAsync(ct);
        var term = DirectorySearchPattern.Literal(query);
        var contains = $"%{term}%";
        return await visible.Where(i => EF.Functions.Like(i.DisplayName, contains, "~") || EF.Functions.Like(i.Affiliation, contains, "~"))
            .OrderByDescending(i => EF.Functions.Like(i.DisplayName, term, "~"))
            .ThenByDescending(i => EF.Functions.Like(i.DisplayName, term + "%", "~"))
            .ThenBy(i => i.DisplayName).ThenBy(i => i.Id).Take(take).ToListAsync(ct);
    }
}
