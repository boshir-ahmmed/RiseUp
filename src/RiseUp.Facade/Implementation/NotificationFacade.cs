using System.Text.RegularExpressions;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public sealed class NotificationFacade(IGovernanceRepository governance) : INotificationFacade
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

    public Task<InboxState> GetInboxAsync(Actor actor, CancellationToken ct = default) =>
        governance.GetInboxAsync(actor.AccountId, actor.TeamAccountId, ct);

    public Task<int> UnreadMessageWatermarkAsync(Actor actor, int conversationId, CancellationToken ct = default) =>
        governance.UnreadMessageWatermarkAsync(actor.AccountId, actor.TeamAccountId, conversationId, ct);

    public Task MarkMessagesReadAsync(Actor actor, int conversationId, int throughId, CancellationToken ct = default) =>
        governance.MarkMessagesReadAsync(actor.AccountId, actor.TeamAccountId, conversationId, throughId, ct);

    public Task MarkAlertsReadAsync(Actor actor, int throughId, CancellationToken ct = default) =>
        governance.MarkAlertsReadAsync(actor.AccountId, actor.TeamAccountId, throughId, ct);
}
