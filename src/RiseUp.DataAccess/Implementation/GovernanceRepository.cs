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

public sealed class GovernanceRepository(RiseUpDbContext db) : IGovernanceRepository
{
    public async Task AddReportAsync(UserReport report, CancellationToken ct = default) => await db.Reports.AddAsync(report, ct);
    public Task<UserReport?> GetReportAsync(int id, CancellationToken ct = default) => db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);
    public async Task<IReadOnlyList<UserReport>> ListReportsAsync(ReportStatus? status, CancellationToken ct = default) =>
        await db.Reports.AsNoTracking().Where(r => status == null || r.Status == status).OrderByDescending(r => r.CreatedAtUtc).Take(200).ToListAsync(ct);
    public async Task AddAuditAsync(AdminActionAudit audit, CancellationToken ct = default) => await db.AdminAudit.AddAsync(audit, ct);
    public async Task<IReadOnlyList<AdminActionAudit>> ListAuditAsync(int take, CancellationToken ct = default) =>
        await db.AdminAudit.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).ThenByDescending(a => a.Id).Take(take).ToListAsync(ct);
    public async Task AddNotificationAsync(Notification notification, CancellationToken ct = default) => await db.Notifications.AddAsync(notification, ct);
    private IQueryable<Notification> Channel(int? accountId, int? teamAccountId, bool messages) =>
        db.Notifications.Where(n => (accountId != null && n.RecipientAccountId == accountId) ||
                (teamAccountId != null && n.RecipientTeamAccountId == teamAccountId))
            // Older chat notifications used the conversation URL without the message anchor.
            .Where(n => (n.Link != null && n.Link.StartsWith("/conversations/") &&
                (n.Link.EndsWith("#msgs") || EF.Functions.Like(n.Text, "New message from %."))) == messages);

    public async Task<IReadOnlyList<Notification>> ListNotificationsAsync(int? accountId, int? teamAccountId, int take, CancellationToken ct = default) =>
        await Channel(accountId, teamAccountId, false).AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id).Take(take).ToListAsync(ct);

    public async Task<InboxState> GetInboxAsync(int? accountId, int? teamAccountId, CancellationToken ct = default)
    {
        var alerts = await Channel(accountId, teamAccountId, false).GroupBy(n => 1)
            .Select(g => new { Latest = g.Max(n => n.Id), Unread = g.Count(n => !n.IsRead) }).SingleOrDefaultAsync(ct);
        var rooms = db.ConversationMembers.Where(m => m.RemovedAtUtc == null &&
            ((accountId != null && m.AccountId == accountId) || (teamAccountId != null && m.TeamAccountId == teamAccountId)))
            .Select(m => m.ConversationId).Distinct();
        var byRoom = await (from n in Channel(accountId, teamAccountId, true)
                            join id in rooms on n.Link!.Replace("#msgs", "") equals "/conversations/" + id
                            group n by id into g
                            select new { Id = g.Key, Latest = g.Max(n => n.Id), Unread = g.Count(n => !n.IsRead) }).ToListAsync(ct);
        return new InboxState(alerts?.Latest ?? 0, byRoom.Select(r => r.Latest).DefaultIfEmpty().Max(), alerts?.Unread ?? 0,
            byRoom.Where(r => r.Unread > 0).ToDictionary(r => r.Id, r => new UnreadConversation(r.Unread, r.Latest)));
    }

    private IQueryable<Notification> UnreadInRoom(int? accountId, int? teamAccountId, int conversationId)
    {
        var link = $"/conversations/{conversationId}";
        return Channel(accountId, teamAccountId, true).Where(n => !n.IsRead && (n.Link == link || n.Link == link + "#msgs"));
    }

    public async Task<int> UnreadMessageWatermarkAsync(int? accountId, int? teamAccountId, int conversationId, CancellationToken ct) =>
        await UnreadInRoom(accountId, teamAccountId, conversationId).MaxAsync(n => (int?)n.Id, ct) ?? 0;

    public Task MarkMessagesReadAsync(int? accountId, int? teamAccountId, int conversationId, int throughId, CancellationToken ct) =>
        UnreadInRoom(accountId, teamAccountId, conversationId).Where(n => n.Id <= throughId)
            .ExecuteUpdateAsync(change => change.SetProperty(n => n.IsRead, true), ct);

    public Task MarkAlertsReadAsync(int? accountId, int? teamAccountId, int throughId, CancellationToken ct) =>
        Channel(accountId, teamAccountId, false).Where(n => !n.IsRead && n.Id <= throughId)
            .ExecuteUpdateAsync(change => change.SetProperty(n => n.IsRead, true), ct);

    private IQueryable<OutboxEvent> DueEmails(DateTime now)
    {
        var retryBefore = now.AddMinutes(-5);
        return db.Outbox.Where(e => e.ProcessedAtUtc == null &&
            (e.UpdatedAtUtc == null || e.UpdatedAtUtc < retryBefore) &&
            (e.Type == "email.verify" || e.Type == "email.password-reset" || e.Type == "notification.email"));
    }

    public async Task<IReadOnlyList<int>> DueEmailsAsync(DateTime now, CancellationToken ct) =>
        await DueEmails(now).OrderBy(e => e.UpdatedAtUtc ?? e.CreatedAtUtc).ThenBy(e => e.Id).Take(40).Select(e => e.Id).ToListAsync(ct);

    public async Task<OutboxEvent?> ClaimEmailAsync(int id, DateTime now, CancellationToken ct)
    {
        var claimed = await DueEmails(now).Where(e => e.Id == id).ExecuteUpdateAsync(
            updates => updates.SetProperty(e => e.UpdatedAtUtc, now).SetProperty(e => e.Attempts, e => e.Attempts + 1), ct);
        return claimed == 1 ? await db.Outbox.SingleAsync(e => e.Id == id, ct) : null;
    }

    public async Task AddOutboxAsync(OutboxEvent evt, CancellationToken ct = default) => await db.Outbox.AddAsync(evt, ct);
}
