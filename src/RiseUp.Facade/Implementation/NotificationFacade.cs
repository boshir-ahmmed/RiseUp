using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public sealed class NotificationFacade(IGovernanceRepository governance, IUnitOfWork uow) : INotificationFacade
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Actor actor, CancellationToken ct = default) =>
        (await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)).Select(n => new NotificationDto(n.Id, n.Text, n.Link, n.IsRead, n.CreatedAtUtc)).ToList();

    public async Task<int> UnreadCountAsync(Actor actor, CancellationToken ct = default) =>
        (await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)).Count(n => !n.IsRead);

    public async Task MarkAllReadAsync(Actor actor, CancellationToken ct = default)
    {
        foreach (var n in await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)) n.IsRead = true;
        await uow.SaveChangesAsync(ct);
    }
}
