using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Conversations;


/// <summary>
/// Membership is verified on EVERY read, send, edit and delete (master prompt §10). Team access derives from
/// currently active membership rows, so removal revokes history immediately (FR-052). No AI ever reads chats (FR-107).
/// </summary>
public sealed class ConversationFacade(IConversationRepository conversations, IConnectionRepository connections, IStartupRepository startups,
    IInvestorRepository investors, ITeamAccountRepository team, IUserAccountRepository accounts, IUnitOfWork uow, IClock clock,
    ActivityRecorder activity, IProfileRepository profiles, NameDirectory names, TeamGroupMembership groupMembership) : IConversationFacade
{
    private MemberKind? KindFor(Actor actor, Conversation c)
    {
        if (actor.IsFounder && actor.StartupId == c.StartupId) return MemberKind.Owner;
        if (actor.IsInvestor && actor.InvestorId == c.InvestorId) return MemberKind.Investor;
        if (actor.IsTeamMember && c.Members.Any(m => m.Kind == MemberKind.Team && m.TeamAccountId == actor.TeamAccountId && m.RemovedAtUtc == null)) return MemberKind.Team;
        return null;
    }

    private async Task<(Conversation? Conversation, MemberKind? Kind)> LoadAsync(Actor actor, int id, CancellationToken ct)
    {
        var c = await conversations.GetWithMembersAsync(id, ct);
        if (c is null) return (null, null);
        var kind = KindFor(actor, c);
        if (kind == MemberKind.Team)
        {
            var t = await team.GetByIdAsync(actor.TeamAccountId!.Value, ct);
            if (t is null || t.Status != TeamAccountStatus.Active) kind = null;
        }
        return (c, kind);
    }

    public async Task<bool> CanAccessAsync(Actor actor, int conversationId, CancellationToken ct = default) => (await LoadAsync(actor, conversationId, ct)).Kind is not null;

    /// <summary>FR-092/108/147/149: blocks and punitive suspensions disable messaging; ordinary lapse does not.</summary>
    private async Task<string?> DisabledReasonAsync(Conversation c, CancellationToken ct)
    {
        var s = await startups.GetByIdAsync(c.StartupId, ct);
        var i = await investors.GetByIdAsync(c.InvestorId, ct);
        if (s?.IsDeleted == true || i?.IsDeleted == true) return "The other party deleted their account. The remaining history is read-only.";
        if (await connections.GetBlockAsync(c.StartupId, c.InvestorId, ct) is not null) return "Messaging is paused because one party blocked the other.";
        if (s?.IsSuspended == true || i?.IsSuspended == true) return "Messaging is disabled while an account is suspended.";
        return null;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(Actor actor, CancellationToken ct = default)
    {
        IReadOnlyList<Conversation> list = actor.IsFounder && actor.StartupId is int sid ? await conversations.ListForStartupAsync(sid, ct)
            : actor.IsInvestor ? await conversations.ListForInvestorAsync(actor.InvestorId!.Value, ct)
            : actor.IsTeamMember ? await conversations.ListForTeamAccountAsync(actor.TeamAccountId!.Value, ct) : [];
        if (actor.IsTeamMember && (await team.GetByIdAsync(actor.TeamAccountId!.Value, ct))?.Status != TeamAccountStatus.Active) return [];
        var result = new List<ConversationSummaryDto>();
        foreach (var c in list)
        {
            var last = await conversations.LatestUserMessageAsync(c.Id, ct);
            result.Add(new ConversationSummaryDto(c.Id, await TitleAsync(actor, c, ct), c.StartupId, c.InvestorId,
                last is null ? null : last.DeletedAtUtc is not null ? "Message deleted" : $"{last.AuthorName}: {last.Body}",
                MemberRow(actor, c)?.Muted ?? false, await DisabledReasonAsync(c, ct)));
        }
        return result;
    }

    /// <summary>FR-094: founder side shows the investor's name; investor side shows the startup's name.</summary>
    private async Task<string> TitleAsync(Actor actor, Conversation c, CancellationToken ct) =>
        actor.IsInvestor ? (await startups.GetByIdAsync(c.StartupId, ct))?.Name ?? "Startup" : (await investors.GetByIdAsync(c.InvestorId, ct))?.DisplayName ?? "Investor";

    private static ConversationMember? MemberRow(Actor actor, Conversation c) => c.Members.FirstOrDefault(m => m.RemovedAtUtc == null &&
        ((m.AccountId != null && m.AccountId == actor.AccountId) || (m.TeamAccountId != null && m.TeamAccountId == actor.TeamAccountId)));

    public Task<Result<ConversationDto>> GetAsync(Actor actor, int conversationId, int page = 1, CancellationToken ct = default) =>
        ReadAsync(actor, conversationId, page, null, ct);

    public Task<Result<ConversationDto>> RecentAsync(Actor actor, int conversationId, int? beforeId, CancellationToken ct = default) =>
        ReadAsync(actor, conversationId, null, beforeId, ct);

    private async Task<Result<ConversationDto>> ReadAsync(Actor actor, int conversationId, int? page, int? beforeId, CancellationToken ct)
    {
        var (c, kind) = await LoadAsync(actor, conversationId, ct);
        if (c is null || kind is null) return Guard.Forbidden("You are not a participant in this conversation.");
        var now = clock.UtcNow;
        IReadOnlyList<Message> messages;
        int? older = null;
        if (page is int legacyPage)
            messages = await conversations.ListMessagesAsync(c.Id, (int)Math.Clamp(((long)legacyPage - 1) * 200, 0, int.MaxValue), 200, ct);
        else
        {
            const int pageSize = 50;
            var recent = await conversations.RecentMessagesAsync(c.Id, beforeId, pageSize + 1, ct);
            messages = recent.Take(pageSize).Reverse().ToList();
            if (recent.Count > pageSize) older = messages[0].Id;
        }
        var startup = await startups.GetByIdAsync(c.StartupId, ct);
        var investor = await investors.GetByIdAsync(c.InvestorId, ct);
        var photoA = await profiles.PhotoIdsForAccountsAsync(messages.Where(m => m.AuthorAccountId != null).Select(m => m.AuthorAccountId!.Value).Distinct().ToList(), ct);
        var photoT = await profiles.PhotoIdsForTeamAsync(messages.Where(m => m.AuthorTeamAccountId != null).Select(m => m.AuthorTeamAccountId!.Value).Distinct().ToList(), ct);
        var dtos = messages.Select(m =>
        {
            var mine = (m.AuthorAccountId != null && m.AuthorAccountId == actor.AccountId) || (m.AuthorTeamAccountId != null && m.AuthorTeamAccountId == actor.TeamAccountId);
            var deleted = m.DeletedAtUtc is not null;
            var author = (m.AuthorKind == MemberKind.Owner && startup?.IsDeleted == true) || (m.AuthorKind == MemberKind.Investor && investor?.IsDeleted == true) ? "Deleted user" : m.AuthorName;
            return new MessageDto(m.Id, m.IsSystem ? "RiseUp" : author, deleted ? "" : m.Body, m.SentAtUtc, mine, m.IsSystem, m.EditedAtUtc is not null, deleted,
                mine && !deleted && now - m.SentAtUtc < BusinessRules.MessageEditWindow, mine && !deleted,
                m.AuthorAccountId is int pa && photoA.TryGetValue(pa, out var p1) ? UrlRewrite.Media(p1) : m.AuthorTeamAccountId is int pt && photoT.TryGetValue(pt, out var p2) ? UrlRewrite.Media(p2) : null);
        }).ToList();
        var members = new List<string>();
        foreach (var m in c.Members.Where(m => m.RemovedAtUtc == null))
            members.Add(m.Kind switch
            {
                MemberKind.Owner => $"{(await accounts.GetByIdAsync(m.AccountId!.Value, ct))?.FullName} (owner)",
                MemberKind.Investor => $"{investor?.DisplayName} (investor)",
                _ => $"{(await team.GetByIdAsync(m.TeamAccountId!.Value, ct))?.FullName} (team)",
            });
        // FR-016/041: contact details only for the two connected principals, never for team members.
        string? email = null, phone = null;
        if (kind is MemberKind.Owner or MemberKind.Investor)
        {
            var otherId = kind == MemberKind.Owner ? investor?.AccountId : startup?.OwnerAccountId;
            if (otherId is int oid && await accounts.GetByIdAsync(oid, ct) is { DeletedAtUtc: null } other) { email = other.Email; phone = other.Phone; }
        }
        var summary = new ConversationSummaryDto(c.Id, await TitleAsync(actor, c, ct), c.StartupId, c.InvestorId, null, MemberRow(actor, c)?.Muted ?? false, await DisabledReasonAsync(c, ct));
        return Result.Success(new ConversationDto(summary, members, dtos, email, phone, older));
    }

    public async Task<Result<MessageDto>> SendAsync(Actor actor, int conversationId, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > BusinessRules.MessageMax)
            return new Error(ErrorCodes.Validation, "Messages must be 1–2,000 characters. Share longer material as an attachment.");
        var (c, kind) = await LoadAsync(actor, conversationId, ct);
        if (c is null || kind is null) return Guard.Forbidden("You are not a participant in this conversation.");
        if (await DisabledReasonAsync(c, ct) is { } reason) return new Error(ErrorCodes.MessagingDisabled, reason);
        var now = clock.UtcNow;
        var authorName = await names.CurrentNameAsync(actor, ct);
        var m = new Message { ConversationId = c.Id, AuthorKind = kind.Value, AuthorAccountId = actor.AccountId, AuthorTeamAccountId = actor.TeamAccountId, AuthorName = authorName, Body = body.Trim(), SentAtUtc = now };
        await conversations.AddMessageAsync(m, ct);
        foreach (var member in c.Members.Where(x => x.RemovedAtUtc == null && !x.Muted && !(x.AccountId != null && x.AccountId == actor.AccountId) && !(x.TeamAccountId != null && x.TeamAccountId == actor.TeamAccountId)))
        {
            if (member.AccountId is int aid) await activity.NotifyAccountAsync(aid, $"New message from {authorName}.", $"/conversations/{c.Id}#msgs", ct);
            else if (member.TeamAccountId is int tid) await activity.NotifyTeamAsync(tid, $"New message from {authorName}.", $"/conversations/{c.Id}#msgs", ct);
        }
        await uow.SaveChangesAsync(ct);
        return Result.Success(new MessageDto(m.Id, m.AuthorName, m.Body, now, true, false, false, false, true, true));
    }

    public async Task<Result> EditAsync(Actor actor, int messageId, string body, CancellationToken ct = default)
    {
        var m = await conversations.GetMessageAsync(messageId, ct);
        if (m is null) return Result.Failure(Guard.NotFound("Message"));
        var (c, kind) = await LoadAsync(actor, m.ConversationId, ct);
        var mine = (m.AuthorAccountId != null && m.AuthorAccountId == actor.AccountId) || (m.AuthorTeamAccountId != null && m.AuthorTeamAccountId == actor.TeamAccountId);
        if (c is null || kind is null || !mine || m.DeletedAtUtc is not null) return Result.Failure(Guard.Forbidden("You can only edit your own messages."));
        if (clock.UtcNow - m.SentAtUtc >= BusinessRules.MessageEditWindow) return Result.Failure(ErrorCodes.EditWindowClosed, "Messages can only be edited for 15 minutes.");
        if (string.IsNullOrWhiteSpace(body) || body.Length > BusinessRules.MessageMax) return Result.Failure(ErrorCodes.Validation, "Messages must be 1–2,000 characters.");
        m.Body = body.Trim(); m.EditedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Message edited.");
    }

    public async Task<Result> DeleteAsync(Actor actor, int messageId, CancellationToken ct = default)
    {
        var m = await conversations.GetMessageAsync(messageId, ct);
        if (m is null) return Result.Failure(Guard.NotFound("Message"));
        var (c, kind) = await LoadAsync(actor, m.ConversationId, ct);
        var mine = (m.AuthorAccountId != null && m.AuthorAccountId == actor.AccountId) || (m.AuthorTeamAccountId != null && m.AuthorTeamAccountId == actor.TeamAccountId);
        if (c is null || kind is null || !mine) return Result.Failure(Guard.Forbidden("You can only delete your own messages."));
        m.DeletedAtUtc = clock.UtcNow;
        await activity.AuditAsync(actor, "Deleted own message for everyone", $"conversation:{c.Id}/message:{m.Id}", "author request", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Message deleted for everyone.");
    }

    /// <summary>FR-048–052: owner-only assignment; new members see full history; investor is notified; removal is immediate.</summary>
    public async Task<Result> SetTeamMembersAsync(Actor owner, int conversationId, IReadOnlyCollection<int> teamAccountIds, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var c = await conversations.GetWithMembersAsync(conversationId, ct);
        if (c is null || !owner.IsFounder || owner.StartupId != c.StartupId) return Result.Failure(Guard.Forbidden("Only the Main Startup Owner can manage members."));
        var eligible = (await team.ListForStartupAsync(c.StartupId, readOnly: false, ct)).Where(t => t.Status == TeamAccountStatus.Active).ToDictionary(t => t.Id);
        var selected = teamAccountIds.ToHashSet();
        if (selected.Any(id => !eligible.ContainsKey(id))) return Result.Failure(ErrorCodes.Validation, "Only your active team accounts can be added.");
        foreach (var m in c.Members.Where(m => m.Kind == MemberKind.Team && m.RemovedAtUtc == null && !selected.Contains(m.TeamAccountId!.Value)))
        {
            m.RemovedAtUtc = clock.UtcNow;
            if (eligible.TryGetValue(m.TeamAccountId!.Value, out var t) && t.GroupMode == TeamGroupMode.All)
                t.GroupMode = TeamGroupMode.AllExcept;
        }
        foreach (var id in selected.Where(id => !c.Members.Any(m => m.TeamAccountId == id && m.RemovedAtUtc == null)))
            await groupMembership.AddAsync(c, eligible[id], ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success("Membership updated.");
    }

    /// <summary>FR-091–093: blocking pauses messaging and prevents new requests; the connection itself is never closed.</summary>
    public async Task<Result> SetBlockedAsync(Actor actor, int conversationId, bool blocked, CancellationToken ct = default)
    {
        var (c, kind) = await LoadAsync(actor, conversationId, ct);
        if (c is null || kind is null or MemberKind.Team) return Result.Failure(Guard.Forbidden());
        var existing = await connections.GetBlockAsync(c.StartupId, c.InvestorId, ct);
        if (blocked && existing is null)
        {
            await connections.AddBlockAsync(new BlockRelationship { StartupId = c.StartupId, InvestorId = c.InvestorId, BlockedBy = kind.Value }, ct);
            foreach (var r in await connections.ListPendingForStartupAsync(c.StartupId, ct))
                if (r.InvestorId == c.InvestorId) { r.Status = RequestStatus.Cancelled; r.DecidedAtUtc = clock.UtcNow; r.DecisionNote = "blocked"; }
        }
        if (!blocked && existing is not null) connections.RemoveBlock(existing);
        await uow.SaveChangesAsync(ct);
        return Result.Success(blocked ? "Blocked. Messaging is paused and new requests are prevented." : "Unblocked. Messaging resumes if no other restriction applies.");
    }

    public async Task<Result> SetMutedAsync(Actor actor, int conversationId, bool muted, CancellationToken ct = default)
    {
        var (c, kind) = await LoadAsync(actor, conversationId, ct);
        if (c is null || kind is null) return Result.Failure(Guard.Forbidden());
        var row = MemberRow(actor, c);
        if (row is not null) row.Muted = muted;
        await uow.SaveChangesAsync(ct);
        return Result.Success(muted ? "Muted." : "Unmuted.");
    }
}
