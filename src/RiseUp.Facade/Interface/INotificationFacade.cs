using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public interface INotificationFacade
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Actor actor, CancellationToken ct = default);
    Task<(int Alert, int Message)> LatestIdsAsync(Actor actor, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Actor actor, CancellationToken ct = default);
    Task MarkAllReadAsync(Actor actor, CancellationToken ct = default);
}
