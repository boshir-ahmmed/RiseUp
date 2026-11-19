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

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetWithMembersAsync(int id, CancellationToken ct = default);
    Task<Conversation?> GetByPairAsync(int startupId, int investorId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListForStartupAsync(int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListForInvestorAsync(int investorId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListForTeamAccountAsync(int teamAccountId, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> ListMessagesAsync(int conversationId, int skip, int take, CancellationToken ct = default);
    Task<Message?> GetMessageAsync(int messageId, CancellationToken ct = default);
    Task AddMessageAsync(Message message, CancellationToken ct = default);
    Task AddMemberAsync(ConversationMember member, CancellationToken ct = default);
}
