using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public interface INotificationFacade
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Actor actor, CancellationToken ct = default);
    Task<InboxState> GetInboxAsync(Actor actor, CancellationToken ct = default);
    Task<int> UnreadMessageWatermarkAsync(Actor actor, int conversationId, CancellationToken ct = default);
    Task MarkMessagesReadAsync(Actor actor, int conversationId, int throughId, CancellationToken ct = default);
    Task MarkAlertsReadAsync(Actor actor, int throughId, CancellationToken ct = default);
}
