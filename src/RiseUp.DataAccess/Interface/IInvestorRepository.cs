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

public interface IInvestorRepository : IRepository<InvestorProfile>
{
    Task<InvestorProfile?> GetByAccountAsync(int accountId, CancellationToken ct = default);
    Task<IReadOnlyList<InvestorProfile>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<InvestorProfile?> GetWithClaimsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InvestorProfile>> ListActiveVerifiedAsync(int take, CancellationToken ct = default);
}
