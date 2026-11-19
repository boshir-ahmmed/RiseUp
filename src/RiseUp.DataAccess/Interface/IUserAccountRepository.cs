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

public interface IUserAccountRepository : IRepository<UserAccount>
{
    Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<IReadOnlyList<UserAccount>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<AccountToken?> FindTokenAsync(string purpose, string tokenHash, CancellationToken ct = default);
    Task AddTokenAsync(AccountToken token, CancellationToken ct = default);
}
