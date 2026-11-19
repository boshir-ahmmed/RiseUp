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

namespace RiseUp.Entities.Abstractions;

public interface IGovernanceRepository
{
    Task AddReportAsync(UserReport report, CancellationToken ct = default);
    Task<UserReport?> GetReportAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<UserReport>> ListReportsAsync(ReportStatus? status, CancellationToken ct = default);
    Task AddAuditAsync(AdminActionAudit audit, CancellationToken ct = default);
    Task<IReadOnlyList<AdminActionAudit>> ListAuditAsync(int take, CancellationToken ct = default);
    Task AddNotificationAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListNotificationsAsync(int? accountId, int? teamAccountId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<int>> DueEmailsAsync(DateTime now, CancellationToken ct);
    Task<OutboxEvent?> ClaimEmailAsync(int id, DateTime now, CancellationToken ct);
    Task AddOutboxAsync(OutboxEvent evt, CancellationToken ct = default);
}
