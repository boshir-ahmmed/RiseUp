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

public sealed class UserAccountRepository(RiseUpDbContext db) : Repository<UserAccount>(db), IUserAccountRepository
{
    public Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);
    public async Task<IReadOnlyList<UserAccount>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync(ct);
    public Task<AccountToken?> FindTokenAsync(string purpose, string tokenHash, CancellationToken ct = default) =>
        Db.AccountTokens.FirstOrDefaultAsync(t => t.Purpose == purpose && t.TokenHash == tokenHash, ct);
    public async Task AddTokenAsync(AccountToken token, CancellationToken ct = default) => await Db.AccountTokens.AddAsync(token, ct);
}
