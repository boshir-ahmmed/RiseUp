using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface IMeetingRepository : IRepository<Meeting>
{
    Task<IReadOnlyList<Meeting>> ListForConversationAsync(int conversationId, CancellationToken ct = default);
}
