using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Conversations;

public interface IConversationFacade
{
    Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(Actor actor, CancellationToken ct = default);
    Task<Result<ConversationDto>> GetAsync(Actor actor, int conversationId, int page = 1, CancellationToken ct = default);
    Task<bool> CanAccessAsync(Actor actor, int conversationId, CancellationToken ct = default);
    Task<Result<MessageDto>> SendAsync(Actor actor, int conversationId, string body, CancellationToken ct = default);
    Task<Result> EditAsync(Actor actor, int messageId, string body, CancellationToken ct = default);
    Task<Result> DeleteAsync(Actor actor, int messageId, CancellationToken ct = default);
    Task<Result> SetTeamMembersAsync(Actor owner, int conversationId, IReadOnlyCollection<int> teamAccountIds, CancellationToken ct = default);
    Task<Result> SetBlockedAsync(Actor actor, int conversationId, bool blocked, CancellationToken ct = default);
    Task<Result> SetMutedAsync(Actor actor, int conversationId, bool muted, CancellationToken ct = default);
}
