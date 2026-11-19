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

public sealed class WalletRepository(RiseUpDbContext db) : Repository<StartupWallet>(db), IWalletRepository
{
    public Task<StartupWallet?> GetByStartupAsync(int startupId, CancellationToken ct = default) => Set.FirstOrDefaultAsync(w => w.StartupId == startupId, ct);
    public Task<bool> LedgerKeyExistsAsync(string idempotencyKey, CancellationToken ct = default) => Db.TokenLedger.AnyAsync(l => l.IdempotencyKey == idempotencyKey, ct);
    public async Task AddLedgerAsync(TokenLedgerEntry entry, CancellationToken ct = default) => await Db.TokenLedger.AddAsync(entry, ct);
    public async Task<IReadOnlyList<TokenLedgerEntry>> ListLedgerAsync(int startupId, int take, CancellationToken ct = default) =>
        await Db.TokenLedger.AsNoTracking().Where(l => l.StartupId == startupId).OrderByDescending(l => l.CreatedAtUtc).ThenByDescending(l => l.Id).Take(take).ToListAsync(ct);
}
