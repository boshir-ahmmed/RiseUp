using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;


/// <summary>
/// Pitch room: only the receiving investor and the owning founder can open it. Engagement is recorded for the receiving investor
/// only. Q&amp;A (max 5 questions while pending) is private and never read by AI. "Request more information" never pauses the
/// 14-day decision window. Accept/decline go through <c>IConnectionRequestFacade.RespondAsync</c> (one connection, one chat).
/// </summary>
public sealed class PitchRoomFacade(IConnectionRepository requests, IPitchRepository pitches, IStartupRepository startups, IInvestorRepository investors,
    IShortlistRepository shortlist, PitchPackageFacade packages, NameDirectory names, IUnitOfWork uow, IClock clock, ActivityRecorder activity) : IPitchRoomFacade
{
    private enum Role { None, Investor, Founder }

    private async Task<(ConnectionRequest? Request, Role Role)> LoadAsync(Actor actor, int requestId, CancellationToken ct)
    {
        var r = await requests.GetByIdAsync(requestId, ct);
        if (r is null || r.Type != RequestType.Pitch) return (null, Role.None);
        if (actor.IsInvestor && actor.InvestorId == r.InvestorId) return (r, Role.Investor);
        if (actor.IsFounder && actor.StartupId == r.StartupId) return (r, Role.Founder);
        return (r, Role.None);
    }

    private static RoomStatsDto Stats(PitchEngagement? e) => new(e?.OpenedAtUtc, e?.PitchWatchPercent ?? 0, e?.DemoWatchPercent ?? 0, e?.SlidesViewed ?? 0, e?.DocumentsViewedCount ?? 0);

    public async Task<Result<PitchRoomDto>> GetAsync(Actor actor, int requestId, CancellationToken ct = default)
    {
        ConnectionRequest? r = null; var role = Role.None;
        if (requestId == 0)
        {
            if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only founders can preview their pitch room.");
            role = Role.Founder;
        }
        else
        {
            (r, role) = await LoadAsync(actor, requestId, ct);
            if (r is null) return Guard.NotFound("Pitch");
            if (role == Role.None) return Guard.Forbidden("Only the receiving investor and the founder can open this pitch room.");
        }
        var startupId = r?.StartupId ?? actor.StartupId!.Value;
        var s = await startups.GetByIdAsync(startupId, ct);
        if (s is null || s.IsDeleted) return Guard.NotFound("Startup");
        var package = await pitches.GetPackageAsync(startupId, ct);
        if (package is null)
        {
            if (role != Role.Founder || requestId != 0) return Guard.NotFound("Pitch package");
            package = new PitchPackage { StartupId = startupId };
        }
        var now = clock.UtcNow;
        PitchEngagement? engagement = null;
        if (r is not null)
        {
            if (r.Status == RequestStatus.Pending && now >= r.ExpiresAtUtc) { r.Status = RequestStatus.Expired; r.DecidedAtUtc = r.ExpiresAtUtc; }
            engagement = await pitches.GetEngagementAsync(r.Id, ct);
            if (role == Role.Investor && engagement?.OpenedAtUtc is null)
            {
                if (engagement is null) { engagement = new PitchEngagement { RequestId = r.Id }; await pitches.AddEngagementAsync(engagement, ct); }
                engagement.OpenedAtUtc = now;
                var investorName = (await investors.GetByIdAsync(r.InvestorId, ct))?.DisplayName;
                await activity.NotifyAccountAsync(s.OwnerAccountId, $"{investorName} opened your pitch room.", $"/pitch-room/{r.Id}", ct);
            }
            try { await uow.SaveChangesAsync(ct); } catch (PersistenceConflictException) { /* concurrent first-open: harmless */ }
        }
        var attached = r is null ? null : r.AttachedDocuments.ToHashSet();
        var dto = await packages.ToDtoAsync(package, s, attached, ct);
        // Investors never see quarantined media.
        if (role == Role.Investor) dto = dto with
        {
            Pitch = dto.Pitch is { Scan: ScanStatus.Clean } ? dto.Pitch : null, Demo = dto.Demo is { Scan: ScanStatus.Clean } ? dto.Demo : null,
            Teaser = dto.Teaser is { Scan: ScanStatus.Clean } ? dto.Teaser : null, DeckFile = dto.DeckFile is { Scan: ScanStatus.Clean } ? dto.DeckFile : null,
            Documents = dto.Documents.Where(d => d.Scan == ScanStatus.Clean).ToList(),
        };
        var questions = r is null ? [] : (await pitches.ListQuestionsAsync(r.Id, ct)).Select(q => new RoomQuestionDto(q.Id, q.AskerName, q.Text, q.CreatedAtUtc, q.Answer, q.AnsweredAtUtc)).ToList();
        var pending = r?.Status == RequestStatus.Pending;
        var investor = r is null ? null : await investors.GetByIdAsync(r.InvestorId, ct);
        var conn = r is null ? null : await requests.GetConnectionAsync(r.StartupId, r.InvestorId, ct);
        var shortlisted = role == Role.Investor && await shortlist.GetAsync(r!.InvestorId, r.StartupId, ct) is not null;
        return Result.Success(new PitchRoomDto(r?.Id, r is null, role == Role.Investor, role == Role.Founder, s.Id, s.Name, s.Tagline, s.Industry, s.Stage, s.Location, s.ColorIndex,
            s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId), s.IsActiveVerified, s.IsSuspended, investor?.DisplayName, r?.SentAtUtc, r?.ExpiresAtUtc, r?.Status,
            r?.PackageVersion, package.PackageVersion, r?.Message, r?.InfoRequestedAtUtc, r?.InfoRequestMessage, dto, questions,
            role == Role.Investor && pending && questions.Count < BusinessRules.PitchQuestionsMax && !s.IsSuspended, Math.Max(0, BusinessRules.PitchQuestionsMax - questions.Count),
            role == Role.Investor && pending && !s.IsSuspended, role == Role.Founder && pending, role == Role.Founder && r is not null ? Stats(engagement) : null,
            shortlisted, conn?.ConversationId));
    }

    public async Task<Result> TrackAsync(Actor actor, int requestId, TrackInput input, CancellationToken ct = default)
    {
        var (r, role) = await LoadAsync(actor, requestId, ct);
        if (r is null || role != Role.Investor) return Result.Success();   // founders/previews are never tracked
        if (r.Status is not (RequestStatus.Pending or RequestStatus.Accepted)) return Result.Success();
        var e = await pitches.GetEngagementAsync(r.Id, ct);
        if (e is null) { e = new PitchEngagement { RequestId = r.Id, OpenedAtUtc = clock.UtcNow }; await pitches.AddEngagementAsync(e, ct); }
        switch (input.Kind)
        {
            case "video" when input.Percent is int pct:
                pct = Math.Clamp(pct, 0, 100);
                if (input.Slot == PitchVideoSlot.Demo) e.DemoWatchPercent = Math.Max(e.DemoWatchPercent, pct); else e.PitchWatchPercent = Math.Max(e.PitchWatchPercent, pct);
                break;
            case "slide" when input.Slide is int i && i >= 0 && i < BusinessRules.DeckSlidesMax:
                e.SlidesViewedMask |= 1L << i;
                break;
            case "doc" when input.DocumentId is int d && r.AttachedDocuments.Contains(d):
                var seen = e.DocumentsViewed.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                if (seen.Add(d.ToString())) e.DocumentsViewed = string.Join(',', seen);
                break;
            default: return Result.Failure(ErrorCodes.Validation, "Unknown tracking event.");
        }
        try { await uow.SaveChangesAsync(ct); } catch (PersistenceConflictException) { /* concurrent progress update: the next tick wins */ }
        return Result.Success();
    }

    public async Task<Result> AskAsync(Actor actor, int requestId, string text, CancellationToken ct = default)
    {
        var (r, role) = await LoadAsync(actor, requestId, ct);
        if (r is null || role != Role.Investor) return Result.Failure(Guard.Forbidden("Only the receiving investor can ask questions."));
        if (r.Status != RequestStatus.Pending || clock.UtcNow >= r.ExpiresAtUtc) return Result.Failure(ErrorCodes.NotPending, "Questions are open only while the pitch is pending.");
        text = (text ?? "").Trim();
        if (text.Length is 0 or > BusinessRules.PitchQuestionMax) return Result.Failure(ErrorCodes.Validation, $"Questions must be 1–{BusinessRules.PitchQuestionMax} characters.");
        if ((await pitches.ListQuestionsAsync(r.Id, ct)).Count >= BusinessRules.PitchQuestionsMax)
            return Result.Failure(ErrorCodes.LimitReached, $"You can ask up to {BusinessRules.PitchQuestionsMax} questions per pitch.");
        var name = await names.CurrentNameAsync(actor, ct);
        // Deliberately NOT sent to the content-safety model: Q&A is private, report-moderated like chat (FR-107).
        await pitches.AddQuestionAsync(new PitchQuestion { RequestId = r.Id, AskedByAccountId = actor.AccountId!.Value, AskerName = name, Text = text }, ct);
        var s = await startups.GetByIdAsync(r.StartupId, ct);
        await activity.NotifyAccountAsync(s!.OwnerAccountId, $"{name} asked a question in your pitch room.", $"/pitch-room/{r.Id}", ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Question sent to the founder.");
    }

    public async Task<Result> AnswerAsync(Actor actor, int questionId, string text, CancellationToken ct = default)
    {
        var q = await pitches.GetQuestionAsync(questionId, ct);
        if (q is null) return Result.Failure(Guard.NotFound("Question"));
        var (r, role) = await LoadAsync(actor, q.RequestId, ct);
        if (r is null || role != Role.Founder) return Result.Failure(Guard.Forbidden("Only the Main Startup Owner can answer pitch questions."));
        if (q.Answer is not null) return Result.Failure(ErrorCodes.Conflict, "This question was already answered.");
        text = (text ?? "").Trim();
        if (text.Length is 0 or > BusinessRules.PitchAnswerMax) return Result.Failure(ErrorCodes.Validation, $"Answers must be 1–{BusinessRules.PitchAnswerMax} characters.");
        q.Answer = text; q.AnsweredAtUtc = clock.UtcNow;
        var investor = await investors.GetByIdAsync(r.InvestorId, ct);
        var s = await startups.GetByIdAsync(r.StartupId, ct);
        await activity.NotifyAccountAsync(investor?.AccountId, $"{s?.Name} answered your question.", $"/pitch-room/{r.Id}", ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Answer posted.");
    }

    public async Task<Result> RequestMoreInfoAsync(Actor actor, int requestId, string message, CancellationToken ct = default)
    {
        var (r, role) = await LoadAsync(actor, requestId, ct);
        if (r is null || role != Role.Investor) return Result.Failure(Guard.Forbidden());
        if (r.Status != RequestStatus.Pending || clock.UtcNow >= r.ExpiresAtUtc) return Result.Failure(ErrorCodes.NotPending, "This pitch is no longer pending.");
        message = (message ?? "").Trim();
        if (message.Length is < 5 or > BusinessRules.MoreInfoMax) return Result.Failure(ErrorCodes.Validation, $"Describe what would help you decide (5–{BusinessRules.MoreInfoMax} characters).");
        r.InfoRequestedAtUtc = clock.UtcNow; r.InfoRequestMessage = message;   // ExpiresAtUtc is deliberately unchanged
        var s = await startups.GetByIdAsync(r.StartupId, ct);
        var name = await names.CurrentNameAsync(actor, ct);
        await activity.NotifyAccountAsync(s!.OwnerAccountId, $"{name} requested more information on your pitch.", $"/pitch-room/{r.Id}", ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Sent to the founder. The pitch stays pending and the 14-day window is unchanged.");
    }

    public async Task<IReadOnlyList<SentPitchDto>> ListSentAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return [];
        var list = (await requests.ListForStartupAsync(actor.StartupId.Value, ct)).Where(r => r.Type == RequestType.Pitch).ToList();
        var ids = list.Select(r => r.Id).ToList();
        var engagement = await pitches.ListEngagementsAsync(ids, ct);
        var questions = await pitches.CountQuestionsAsync(ids, ct);
        var names = (await investors.GetManyAsync(list.Select(r => r.InvestorId).Distinct().ToList(), ct)).ToDictionary(i => i.Id, i => i.DisplayName);
        return list.Select(r => new SentPitchDto(r.Id, r.InvestorId, names.GetValueOrDefault(r.InvestorId, "Investor"), r.SentAtUtc, r.ExpiresAtUtc, r.Status, r.PackageVersion,
            r.AttachedDocuments.Count(), Stats(engagement.GetValueOrDefault(r.Id)), questions.GetValueOrDefault(r.Id).Total, questions.GetValueOrDefault(r.Id).Unanswered,
            r.InfoRequestedAtUtc is not null)).ToList();
    }
}
