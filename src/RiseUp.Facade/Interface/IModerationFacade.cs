using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Moderation;

public interface IModerationFacade
{
    Task<Result<int>> ReportAsync(Actor actor, ReportKind kind, string target, string reason, int? conversationId = null, int? messageId = null, CancellationToken ct = default);
    Task<IReadOnlyList<ReportDto>> ListReportsAsync(Actor moderator, CancellationToken ct = default);
    /// <summary>FR-106/156: report-scoped, reason-required, audited access to exactly one reported message.</summary>
    Task<Result<string>> InspectReportedMessageAsync(Actor moderator, int reportId, string reason, CancellationToken ct = default);
    Task<Result> ResolveAsync(Actor moderator, int reportId, bool actioned, string note, CancellationToken ct = default);
    Task<IReadOnlyList<AuditDto>> ListAuditAsync(Actor superAdmin, CancellationToken ct = default);
}
