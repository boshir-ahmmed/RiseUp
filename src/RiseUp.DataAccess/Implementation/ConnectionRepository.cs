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

public sealed class ConnectionRepository(RiseUpDbContext db) : Repository<ConnectionRequest>(db), IConnectionRepository
{
    public Task<ConnectionRequest?> GetPendingForPairAsync(int startupId, int investorId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(r => r.StartupId == startupId && r.InvestorId == investorId && r.Status == RequestStatus.Pending, ct);
    public Task<ConnectionRequest?> GetActiveCooldownAsync(int startupId, int investorId, RequestType type, DateTime nowUtc, CancellationToken ct = default) =>
        Set.Where(r => r.StartupId == startupId && r.InvestorId == investorId && r.Type == type && r.Status == RequestStatus.Rejected && r.ExpiresAtUtc > nowUtc)
           .OrderByDescending(r => r.ExpiresAtUtc).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListForStartupAsync(int startupId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(r => r.StartupId == startupId).OrderByDescending(r => r.SentAtUtc).Take(200).ToListAsync(ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListForInvestorAsync(int investorId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(r => r.InvestorId == investorId).OrderByDescending(r => r.SentAtUtc).Take(200).ToListAsync(ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListPendingDueAsync(DateTime nowUtc, int take, CancellationToken ct = default) =>
        await Set.Where(r => r.Status == RequestStatus.Pending && r.ExpiresAtUtc <= nowUtc).OrderBy(r => r.ExpiresAtUtc).Take(take).ToListAsync(ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListPendingForStartupAsync(int startupId, CancellationToken ct = default) =>
        await Set.Where(r => r.StartupId == startupId && r.Status == RequestStatus.Pending).ToListAsync(ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListPendingForInvestorAsync(int investorId, CancellationToken ct = default) =>
        await Set.Where(r => r.InvestorId == investorId && r.Status == RequestStatus.Pending).ToListAsync(ct);
    public async Task<IReadOnlyList<StartupInvestorConnection>> ListStartupConnectionsAsync(int startupId, CancellationToken ct) =>
        await Db.Connections.AsNoTracking().Where(c => c.StartupId == startupId)
            .OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<StartupInvestorConnection>> ListInvestorConnectionsAsync(int investorId, CancellationToken ct) =>
        await Db.Connections.AsNoTracking().Where(c => c.InvestorId == investorId)
            .OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id).ToListAsync(ct);

    public Task<StartupInvestorConnection?> GetConnectionAsync(int startupId, int investorId, CancellationToken ct = default) =>
        Db.Connections.FirstOrDefaultAsync(c => c.StartupId == startupId && c.InvestorId == investorId, ct);
    public async Task AddConnectionAsync(StartupInvestorConnection connection, CancellationToken ct = default) => await Db.Connections.AddAsync(connection, ct);
    public Task<BlockRelationship?> GetBlockAsync(int startupId, int investorId, CancellationToken ct = default) =>
        Db.Blocks.FirstOrDefaultAsync(b => b.StartupId == startupId && b.InvestorId == investorId, ct);
    public async Task AddBlockAsync(BlockRelationship block, CancellationToken ct = default) => await Db.Blocks.AddAsync(block, ct);
    public void RemoveBlock(BlockRelationship block) => Db.Blocks.Remove(block);
    public Task<InvitationDailyCounter?> GetCounterAsync(int investorId, DateOnly dhakaDate, CancellationToken ct = default) =>
        Db.InvitationDailyCounters.FirstOrDefaultAsync(c => c.InvestorId == investorId && c.DhakaDate == dhakaDate, ct);
    public async Task AddCounterAsync(InvitationDailyCounter counter, CancellationToken ct = default) => await Db.InvitationDailyCounters.AddAsync(counter, ct);
    public Task<PitchDraft?> GetDraftAsync(int draftId, CancellationToken ct = default) => Db.PitchDrafts.FirstOrDefaultAsync(d => d.Id == draftId, ct);
    public async Task<IReadOnlyList<PitchDraft>> ListDraftsAsync(int startupId, CancellationToken ct = default) =>
        await Db.PitchDrafts.AsNoTracking().Where(d => d.StartupId == startupId).OrderByDescending(d => d.Id).ToListAsync(ct);
    public async Task AddDraftAsync(PitchDraft draft, CancellationToken ct = default) => await Db.PitchDrafts.AddAsync(draft, ct);
    public void RemoveDraft(PitchDraft draft) => Db.PitchDrafts.Remove(draft);
}
