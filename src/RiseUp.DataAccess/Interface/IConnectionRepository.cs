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

public interface IConnectionRepository : IRepository<ConnectionRequest>
{
    Task<ConnectionRequest?> GetPendingForPairAsync(int startupId, int investorId, CancellationToken ct = default);
    Task<ConnectionRequest?> GetActiveCooldownAsync(int startupId, int investorId, RequestType type, DateTime nowUtc, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListForStartupAsync(int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListForInvestorAsync(int investorId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListPendingDueAsync(DateTime nowUtc, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListPendingForStartupAsync(int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListPendingForInvestorAsync(int investorId, CancellationToken ct = default);
    Task<StartupInvestorConnection?> GetConnectionAsync(int startupId, int investorId, CancellationToken ct = default);
    Task AddConnectionAsync(StartupInvestorConnection connection, CancellationToken ct = default);
    Task<BlockRelationship?> GetBlockAsync(int startupId, int investorId, CancellationToken ct = default);
    Task AddBlockAsync(BlockRelationship block, CancellationToken ct = default);
    void RemoveBlock(BlockRelationship block);
    Task<InvitationDailyCounter?> GetCounterAsync(int investorId, DateOnly dhakaDate, CancellationToken ct = default);
    Task AddCounterAsync(InvitationDailyCounter counter, CancellationToken ct = default);
    Task<PitchDraft?> GetDraftAsync(int draftId, CancellationToken ct = default);
    Task<IReadOnlyList<PitchDraft>> ListDraftsAsync(int startupId, CancellationToken ct = default);
    Task AddDraftAsync(PitchDraft draft, CancellationToken ct = default);
    void RemoveDraft(PitchDraft draft);
}
