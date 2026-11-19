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

public interface IWalletRepository : IRepository<StartupWallet>
{
    Task<StartupWallet?> GetByStartupAsync(int startupId, CancellationToken ct = default);
    Task<TokenLedgerEntry?> GetLedgerByKeyAsync(string key, CancellationToken ct);
    Task<bool> LedgerKeyExistsAsync(string idempotencyKey, CancellationToken ct = default);
    Task AddLedgerAsync(TokenLedgerEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<TokenLedgerEntry>> ListLedgerAsync(int startupId, int take, CancellationToken ct = default);
}
