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
    public async Task<IReadOnlyList<Notification>> ListNotificationsAsync(int? accountId, int? teamAccountId, int take, CancellationToken ct = default) =>
        await db.Notifications.Where(n => (accountId != null && n.RecipientAccountId == accountId) || (teamAccountId != null && n.RecipientTeamAccountId == teamAccountId))
            .OrderByDescending(n => n.CreatedAtUtc).Take(take).ToListAsync(ct);
    public async Task AddOutboxAsync(OutboxEvent evt, CancellationToken ct = default) => await db.Outbox.AddAsync(evt, ct);
}
