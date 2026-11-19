using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;


/// <summary>
/// Meeting scheduling inside a connected conversation: the owner or investor proposes up to 3 future slots (within 90 days),
/// the OTHER side confirms one. Team members can see meetings but not schedule them. The call itself happens outside RiseUp.
/// </summary>
public sealed class MeetingFacade(IMeetingRepository meetings, IConversationRepository conversations, IConnectionRepository connections,
    IStartupRepository startups, IInvestorRepository investors, NameDirectory names, IUnitOfWork uow, IClock clock, ActivityRecorder activity) : IMeetingFacade
{
    private async Task<(Conversation? C, MemberKind? Kind)> AccessAsync(Actor actor, int conversationId, CancellationToken ct)
    {
        var c = await conversations.GetWithMembersAsync(conversationId, ct);
        if (c is null) return (null, null);
        if (actor.IsFounder && actor.StartupId == c.StartupId) return (c, MemberKind.Owner);
        if (actor.IsInvestor && actor.InvestorId == c.InvestorId) return (c, MemberKind.Investor);
        if (actor.IsTeamMember && c.Members.Any(m => m.TeamAccountId == actor.TeamAccountId && m.RemovedAtUtc == null)) return (c, MemberKind.Team);
        return (c, null);
    }

    private async Task<string?> BlockedAsync(Conversation c, CancellationToken ct)
    {
        if (await connections.GetConnectionAsync(c.StartupId, c.InvestorId, ct) is null) return "Meetings are available only between connected parties.";
        if (await connections.GetBlockAsync(c.StartupId, c.InvestorId, ct) is not null) return "Scheduling is paused because one party blocked the other.";
        var s = await startups.GetByIdAsync(c.StartupId, ct); var i = await investors.GetByIdAsync(c.InvestorId, ct);
        if (s is null || i is null || s.IsDeleted || i.IsDeleted || s.IsSuspended || i.IsSuspended) return "Scheduling is disabled for this conversation.";
        return null;
    }

    public async Task<IReadOnlyList<MeetingDto>> ListAsync(Actor actor, int conversationId, CancellationToken ct = default)
    {
        var (c, kind) = await AccessAsync(actor, conversationId, ct);
        if (c is null || kind is null) return [];
        return (await meetings.ListForConversationAsync(c.Id, ct)).Select(m => new MeetingDto(m.Id, m.ProposedByName, m.ProposedByKind == kind, m.Slots.ToList(), m.Agenda, m.Status,
            m.ConfirmedSlotUtc, m.Status == MeetingStatus.Proposed && kind != MemberKind.Team && kind != m.ProposedByKind && m.Slots.Any(s => s > clock.UtcNow),
            m.Status != MeetingStatus.Cancelled && kind == m.ProposedByKind)).ToList();
    }

    public async Task<Result<int>> ProposeAsync(Actor actor, int conversationId, IReadOnlyList<DateTime> slotsUtc, string? agenda, CancellationToken ct = default)
    {
        var (c, kind) = await AccessAsync(actor, conversationId, ct);
        if (c is null || kind is null) return Guard.Forbidden("You are not a participant in this conversation.");
        if (kind == MemberKind.Team) return Guard.Forbidden("Only the startup owner and the investor can schedule meetings.");
        if (await BlockedAsync(c, ct) is { } blocked) return new Error(ErrorCodes.NotConnected, blocked);
        var now = clock.UtcNow;
        var slots = slotsUtc.Select(s => DateTime.SpecifyKind(s, DateTimeKind.Utc)).Distinct().OrderBy(s => s).ToList();
        if (slots.Count is 0 or > BusinessRules.MeetingSlotsMax) return new Error(ErrorCodes.Validation, $"Offer 1–{BusinessRules.MeetingSlotsMax} time slots.");
        if (slots.Any(s => s <= now.AddMinutes(30))) return new Error(ErrorCodes.Validation, "Slots must be at least 30 minutes in the future.");
        if (slots.Any(s => s > now + BusinessRules.MeetingHorizon)) return new Error(ErrorCodes.Validation, "Slots must be within the next 90 days.");
        agenda = (agenda ?? "").Trim();
        if (agenda.Length > 140) return new Error(ErrorCodes.Validation, "Agenda is limited to 140 characters.");
        var name = await names.CurrentNameAsync(actor, ct);
        var meeting = new Meeting
        {
            ConversationId = c.Id, ProposedByKind = kind.Value, ProposedByAccountId = actor.AccountId, ProposedByName = name, Agenda = agenda,
            Slot1Utc = slots[0], Slot2Utc = slots.Count > 1 ? slots[1] : null, Slot3Utc = slots.Count > 2 ? slots[2] : null,
            Status = MeetingStatus.Proposed,
        };
        await meetings.AddAsync(meeting, ct);
        await conversations.AddMessageAsync(new Message { ConversationId = c.Id, AuthorKind = kind.Value, AuthorName = "RiseUp", IsSystem = true, SentAtUtc = now,
            Body = $"{name} proposed a meeting ({slots.Count} time slot{(slots.Count == 1 ? "" : "s")}). The call happens on your own video tool — RiseUp does not host calls." }, ct);
        await NotifyOtherAsync(c, kind.Value, $"{name} proposed a meeting.", ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(meeting.Id, "Meeting proposal sent.");
    }

    public async Task<Result> AcceptAsync(Actor actor, int meetingId, DateTime slotUtc, CancellationToken ct = default)
    {
        var m = await meetings.GetByIdAsync(meetingId, ct);
        if (m is null) return Result.Failure(Guard.NotFound("Meeting"));
        var (c, kind) = await AccessAsync(actor, m.ConversationId, ct);
        if (c is null || kind is null or MemberKind.Team) return Result.Failure(Guard.Forbidden());
        if (kind == m.ProposedByKind) return Result.Failure(Guard.Forbidden("The other side confirms the meeting."));
        if (m.Status != MeetingStatus.Proposed) return Result.Failure(ErrorCodes.NotPending, "This meeting is no longer open.");
        if (await BlockedAsync(c, ct) is { } blocked) return Result.Failure(ErrorCodes.NotConnected, blocked);
        slotUtc = DateTime.SpecifyKind(slotUtc, DateTimeKind.Utc);
        if (!m.Slots.Contains(slotUtc)) return Result.Failure(ErrorCodes.Validation, "Pick one of the proposed slots.");
        if (slotUtc <= clock.UtcNow) return Result.Failure(ErrorCodes.Validation, "That slot is in the past. Ask for new times.");
        m.Status = MeetingStatus.Confirmed; m.ConfirmedSlotUtc = slotUtc;
        var name = await names.CurrentNameAsync(actor, ct);
        await conversations.AddMessageAsync(new Message { ConversationId = c.Id, AuthorKind = kind.Value, AuthorName = "RiseUp", IsSystem = true, SentAtUtc = clock.UtcNow,
            Body = $"Meeting confirmed for {DhakaTime.ToDhaka(slotUtc):ddd dd MMM yyyy, HH:mm} (Dhaka)." }, ct);
        await NotifyOtherAsync(c, kind.Value, $"{name} confirmed the meeting for {DhakaTime.ToDhaka(slotUtc):dd MMM, HH:mm} (Dhaka).", ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Meeting confirmed. Add it to your calendar.");
    }

    public async Task<Result> CancelAsync(Actor actor, int meetingId, CancellationToken ct = default)
    {
        var m = await meetings.GetByIdAsync(meetingId, ct);
        if (m is null) return Result.Failure(Guard.NotFound("Meeting"));
        var (c, kind) = await AccessAsync(actor, m.ConversationId, ct);
        if (c is null || kind != m.ProposedByKind) return Result.Failure(Guard.Forbidden("Only the person who proposed the meeting can cancel it."));
        if (m.Status == MeetingStatus.Cancelled) return Result.Success("Already cancelled.");
        m.Status = MeetingStatus.Cancelled;
        await conversations.AddMessageAsync(new Message { ConversationId = c.Id, AuthorKind = kind.Value, AuthorName = "RiseUp", IsSystem = true, SentAtUtc = clock.UtcNow, Body = $"{m.ProposedByName} cancelled the meeting proposal." }, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Meeting cancelled.");
    }

    public async Task<Result<(DateTime StartUtc, string Title, string Agenda)>> GetCalendarAsync(Actor actor, int meetingId, CancellationToken ct = default)
    {
        var m = await meetings.GetByIdAsync(meetingId, ct);
        if (m is null || m.Status != MeetingStatus.Confirmed) return Guard.NotFound("Confirmed meeting");
        var (c, kind) = await AccessAsync(actor, m.ConversationId, ct);
        if (c is null || kind is null) return Guard.Forbidden();
        var s = await startups.GetByIdAsync(c.StartupId, ct); var i = await investors.GetByIdAsync(c.InvestorId, ct);
        return Result.Success((m.ConfirmedSlotUtc!.Value, $"RiseUp meeting: {s?.Name} × {i?.DisplayName}", m.Agenda));
    }

    private async Task NotifyOtherAsync(Conversation c, MemberKind from, string text, CancellationToken ct)
    {
        var target = from == MemberKind.Investor ? (await startups.GetByIdAsync(c.StartupId, ct))?.OwnerAccountId : (await investors.GetByIdAsync(c.InvestorId, ct))?.AccountId;
        await activity.NotifyAccountAsync(target, text, $"/conversations/{c.Id}", ct);
    }
}
