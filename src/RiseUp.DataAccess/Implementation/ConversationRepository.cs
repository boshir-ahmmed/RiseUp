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

public sealed class ConversationRepository(RiseUpDbContext db) : Repository<Conversation>(db), IConversationRepository
{
    public Task<Conversation?> GetWithMembersAsync(int id, CancellationToken ct = default) =>
        Set.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<Conversation?> GetByPairAsync(int startupId, int investorId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(c => c.StartupId == startupId && c.InvestorId == investorId, ct);
    public async Task<IReadOnlyList<Conversation>> ListForStartupAsync(int startupId, CancellationToken ct = default) =>
        await Set.Include(c => c.Members).AsNoTracking().Where(c => c.StartupId == startupId).ToListAsync(ct);
    public async Task<IReadOnlyList<Conversation>> ListForInvestorAsync(int investorId, CancellationToken ct = default) =>
        await Set.Include(c => c.Members).AsNoTracking().Where(c => c.InvestorId == investorId).ToListAsync(ct);
    public async Task<IReadOnlyList<Conversation>> ListForTeamAccountAsync(int teamAccountId, CancellationToken ct = default) =>
        await Set.Include(c => c.Members).AsNoTracking()
            .Where(c => c.Members.Any(m => m.TeamAccountId == teamAccountId && m.RemovedAtUtc == null)).ToListAsync(ct);
    public async Task<IReadOnlyList<Message>> ListMessagesAsync(int conversationId, int skip, int take, CancellationToken ct = default) =>
        await Db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId).OrderBy(m => m.SentAtUtc).ThenBy(m => m.Id).Skip(skip).Take(take).ToListAsync(ct);
    public Task<Message?> LatestUserMessageAsync(int conversationId, CancellationToken ct) =>
        Db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId && !m.IsSystem)
            .OrderByDescending(m => m.Id).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<Message>> RecentMessagesAsync(int conversationId, int? beforeId, int take, CancellationToken ct) =>
        await Db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId && (beforeId == null || m.Id < beforeId))
            .OrderByDescending(m => m.Id).Take(take).ToListAsync(ct);
    public Task<Message?> GetMessageAsync(int messageId, CancellationToken ct = default) => Db.Messages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
    public async Task AddMessageAsync(Message message, CancellationToken ct = default) => await Db.Messages.AddAsync(message, ct);
    public async Task AddMemberAsync(ConversationMember member, CancellationToken ct = default) => await Db.ConversationMembers.AddAsync(member, ct);
}
