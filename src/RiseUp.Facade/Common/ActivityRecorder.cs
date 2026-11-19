using System.Text.Json;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Governance;

namespace RiseUp.Facade.Common;

/// <summary>Writes in-app notifications, outbox events and audit rows inside the caller's unit of work.</summary>
public sealed class ActivityRecorder(IGovernanceRepository governance)
{
    public async Task NotifyAccountAsync(int? accountId, string text, string? link = null, CancellationToken ct = default)
    {
        if (accountId is null) return;
        await governance.AddNotificationAsync(new Notification { RecipientAccountId = accountId, Text = text, Link = link }, ct);
        await governance.AddOutboxAsync(new OutboxEvent { Type = "notification.email", Payload = JsonSerializer.Serialize(new { accountId, text, link }) }, ct);
    }

    public Task NotifyTeamAsync(int teamAccountId, string text, string? link = null, CancellationToken ct = default) =>
        governance.AddNotificationAsync(new Notification { RecipientTeamAccountId = teamAccountId, Text = text, Link = link }, ct);

    public Task AuditAsync(Actor actor, string action, string target, string reason, bool restrictedContent = false, CancellationToken ct = default) =>
        governance.AddAuditAsync(new AdminActionAudit
        {
            ActorAccountId = actor.AccountId, ActorRole = actor.AdminRole == Entities.Common.AdminRole.None ? actor.Role?.ToString() ?? "system" : actor.AdminRole.ToString(),
            Action = action, Target = target, Reason = reason, IsRestrictedContentAccess = restrictedContent,
        }, ct);

    public Task OutboxAsync(string type, object payload, CancellationToken ct = default) =>
        governance.AddOutboxAsync(new OutboxEvent { Type = type, Payload = JsonSerializer.Serialize(payload) }, ct);
}
