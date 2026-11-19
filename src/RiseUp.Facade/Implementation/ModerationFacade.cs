using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Moderation;

public sealed class ModerationFacade(IGovernanceRepository governance, IConversationRepository conversations, IUnitOfWork uow, ActivityRecorder activity) : IModerationFacade
{
    public async Task<Result<int>> ReportAsync(Actor actor, ReportKind kind, string target, string reason, int? conversationId = null, int? messageId = null, CancellationToken ct = default)
    {
        if (actor.AccountId is null && actor.TeamAccountId is null) return new Error(ErrorCodes.Unauthenticated, "Sign in to report content.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) return new Error(ErrorCodes.Validation, "Describe the problem (max 500 characters).");
        if (kind == ReportKind.GroupMessage && (conversationId is null || messageId is null)) return new Error(ErrorCodes.Validation, "Missing message reference.");
        var report = new UserReport { Kind = kind, TargetReference = target, Reason = reason.Trim(), ReporterAccountId = actor.AccountId ?? 0, ConversationId = conversationId, MessageId = messageId, Status = ReportStatus.Open };
        await governance.AddReportAsync(report, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(report.Id, "Report sent to Moderators. Reported content is reviewed by a person, not AI.");
    }

    public async Task<IReadOnlyList<ReportDto>> ListReportsAsync(Actor moderator, CancellationToken ct = default) =>
        !moderator.IsAdmin(AdminRole.Moderator) ? [] :
        (await governance.ListReportsAsync(null, ct)).Select(r => new ReportDto(r.Id, r.Kind, r.TargetReference, r.Reason, r.Status, r.CreatedAtUtc, r.ResolutionNote)).ToList();

    public async Task<Result<string>> InspectReportedMessageAsync(Actor moderator, int reportId, string reason, CancellationToken ct = default)
    {
        if (!moderator.IsAdmin(AdminRole.Moderator)) return Guard.Forbidden();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return new Error(ErrorCodes.Validation, "An investigation reason is required.");
        var report = await governance.GetReportAsync(reportId, ct);
        if (report is null || report.Kind != ReportKind.GroupMessage || report.MessageId is null) return Guard.NotFound("Reported message");
        var message = await conversations.GetMessageAsync(report.MessageId.Value, ct);
        if (message is null || message.ConversationId != report.ConversationId) return Guard.NotFound("Reported message");
        await activity.AuditAsync(moderator, "Restricted content access · reported message only", $"conversation:{message.ConversationId}/message:{message.Id}", reason.Trim(), restrictedContent: true, ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success<string>(message.DeletedAtUtc is not null ? "[deleted by author]" : $"{message.AuthorName}: {message.Body}");
    }

    public async Task<Result> ResolveAsync(Actor moderator, int reportId, bool actioned, string note, CancellationToken ct = default)
    {
        if (!moderator.IsAdmin(AdminRole.Moderator)) return Result.Failure(Guard.Forbidden());
        var report = await governance.GetReportAsync(reportId, ct);
        if (report is null) return Result.Failure(Guard.NotFound("Report"));
        report.Status = actioned ? ReportStatus.Actioned : ReportStatus.Dismissed; report.ResolutionNote = note;
        await activity.AuditAsync(moderator, actioned ? "Actioned report" : "Dismissed report", $"report:{report.Id}", note, ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Report resolved.");
    }

    public async Task<IReadOnlyList<AuditDto>> ListAuditAsync(Actor superAdmin, CancellationToken ct = default) =>
        !superAdmin.IsAdmin(AdminRole.SuperAdmin) ? [] :
        (await governance.ListAuditAsync(200, ct)).Select(a => new AuditDto(a.CreatedAtUtc, a.ActorRole, a.Action, a.Target, a.Reason, a.IsRestrictedContentAccess)).ToList();
}
