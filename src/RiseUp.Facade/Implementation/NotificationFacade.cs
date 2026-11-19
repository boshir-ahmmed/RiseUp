using System.Text.RegularExpressions;
using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public sealed class NotificationFacade(IGovernanceRepository governance, IUnitOfWork uow) : INotificationFacade
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Actor actor, CancellationToken ct = default) =>
        (await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)).Select(n => new NotificationDto(n.Id, DisplayText(n.Text, n.Link), n.Link, n.IsRead, n.CreatedAtUtc)).ToList();

    // Match only the old system templates. Profile names and message content may also contain "tokens".
    private static string DisplayText(string text, string? link) => link switch
    {
        "/wallet" => Regex.Replace(text, @"^Payment received: (\d+) tokens added\.\z", "Payment received: $1 credits added."),
        "/dashboard" when text.EndsWith(" Welcome tokens credited (one-time).", StringComparison.Ordinal) =>
            Regex.Replace(text, @"( verified — \d+) Welcome tokens credited \(one-time\)\.\z", "$1 welcome credits added (one-time)."),
        "/dashboard" => Regex.Replace(text, @" re-verified — no additional Welcome tokens\.\z", " re-verified — no additional welcome credits."),
        _ => text,
    };

    public async Task<int> UnreadCountAsync(Actor actor, CancellationToken ct = default) =>
        (await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)).Count(n => !n.IsRead);

    public async Task MarkAllReadAsync(Actor actor, CancellationToken ct = default)
    {
        foreach (var n in await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)) n.IsRead = true;
        await uow.SaveChangesAsync(ct);
    }
}
