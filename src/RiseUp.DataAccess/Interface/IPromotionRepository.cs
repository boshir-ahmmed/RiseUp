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

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<Promotion?> GetOpenForPostAsync(int postId, CancellationToken ct = default);
    Task<IReadOnlyList<Promotion>> ListByStatusAsync(PromotionStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Promotion>> ListForStartupAsync(int startupId, CancellationToken ct = default);
}
