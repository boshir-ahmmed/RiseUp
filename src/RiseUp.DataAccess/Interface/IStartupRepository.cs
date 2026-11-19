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

public interface IStartupRepository : IRepository<Startup>
{
    Task<Startup?> GetByOwnerAsync(int ownerAccountId, CancellationToken ct = default);
    Task<IReadOnlyList<Startup>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<Startup?> GetWithMetricsAsync(int id, CancellationToken ct = default);
    Task<bool> LegalNameExistsAsync(string normalizedLegalName, int? excludeId, CancellationToken ct = default);
    Task<(IReadOnlyList<Startup> Items, int Total)> SearchAsync(StartupSearch search, CancellationToken ct = default);
    Task<IReadOnlyList<Startup>> ListActiveVerifiedAsync(int take, CancellationToken ct = default);
    Task<bool> HasViewSinceAsync(int startupId, string viewerKey, DateTime sinceUtc, CancellationToken ct = default);
    Task AddViewAsync(ProfileViewEvent view, CancellationToken ct = default);
}
