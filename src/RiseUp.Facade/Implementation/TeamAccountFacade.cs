using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.TeamAccounts;

public sealed class TeamAccountFacade(ITeamAccountRepository team, IConversationRepository conversations, TeamGroupMembership membership,
    IPasswordHasher hasher, IUnitOfWork uow, IClock clock, IValidator<CreateTeamAccountRequest> validator) : ITeamAccountFacade
{
    private async Task<TeamAccount?> OwnedAsync(Actor owner, int id, CancellationToken ct)
    {
        if (!owner.IsFounder || owner.StartupId is null) return null;
        var t = await team.GetByIdAsync(id, ct);
        return t is not null && t.StartupId == owner.StartupId && t.Status != TeamAccountStatus.Deleted ? t : null;
    }

    public async Task<IReadOnlyList<TeamAccountDto>> ListAsync(Actor owner, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return [];
        var convs = await conversations.ListForStartupAsync(owner.StartupId.Value, ct);
        return (await team.ListForStartupAsync(owner.StartupId.Value, readOnly: true, ct)).Select(t =>
        {
            var held = convs.Where(c => HasMember(c, t.Id)).Select(c => c.Id).ToList();
            var choices = t.Status == TeamAccountStatus.Deactivated ? SavedChoices(t) : GroupChoices(t.GroupMode, convs, held);
            return new TeamAccountDto(t.Id, t.FullName, t.Designation, t.Username, t.Email, t.Status, held, t.LinkedInUrl, t.GroupMode, choices);
        }).ToList();
    }

    private static bool HasMember(Conversation c, int id) => c.Members.Any(m => m.TeamAccountId == id && m.RemovedAtUtc == null);

    private static List<int> SavedChoices(TeamAccount t) =>
        t.PreviousConversationIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

    private static List<int> GroupChoices(TeamGroupMode mode, IReadOnlyList<Conversation> groups, List<int> held) => mode switch
    {
        TeamGroupMode.All => [],
        TeamGroupMode.AllExcept => groups.Select(c => c.Id).Except(held).ToList(),
        _ => held,
    };

    private async Task ApplyGroupsAsync(TeamAccount t, IReadOnlyList<Conversation> groups, IReadOnlyCollection<int> choices, CancellationToken ct)
    {
        var selected = choices.ToHashSet();
        if (t.Status == TeamAccountStatus.Deactivated)
            t.PreviousConversationIds = t.GroupMode == TeamGroupMode.All ? "" : string.Join(',', selected.Order());
        foreach (var group in groups)
        {
            var wanted = t.Status == TeamAccountStatus.Active && (t.GroupMode switch
            {
                TeamGroupMode.All => true,
                TeamGroupMode.AllExcept => !selected.Contains(group.Id),
                _ => selected.Contains(group.Id),
            });
            if (HasMember(group, t.Id) == wanted) continue;
            if (wanted) await membership.AddAsync(group, t, ct);
            else
            {
                var tracked = await conversations.GetWithMembersAsync(group.Id, ct);
                foreach (var row in tracked!.Members.Where(m => m.TeamAccountId == t.Id && m.RemovedAtUtc == null))
                    row.RemovedAtUtc = clock.UtcNow;
            }
        }
    }

    private static void SetDetails(TeamAccount t, CreateTeamAccountRequest request)
    {
        t.FullName = request.FullName.Trim();
        t.Designation = request.Designation.Trim();
        t.Username = request.Username;
        t.Email = request.Email.Trim();
        t.LinkedInUrl = request.LinkedInUrl?.Trim();
        t.GroupMode = request.GroupMode;
    }

    private static Error Conflict(PersistenceConflictException ex) =>
        ex.ContactError ?? new Error(ErrorCodes.Conflict, "The member could not be saved. Check that the username and email are unique and try again.");

    private static bool ValidGroups(CreateTeamAccountRequest request, IReadOnlyList<Conversation> groups) =>
        !(request.GroupIds ?? []).Except(groups.Select(c => c.Id)).Any();

    public async Task<Result<OneTimeCredential>> CreateAsync(Actor owner, CreateTeamAccountRequest request, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can create team accounts.");
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return invalid;
        await using var tx = await uow.BeginTransactionAsync(ct);
        if (await team.CountOccupyingSlotsAsync(owner.StartupId.Value, ct) >= BusinessRules.MaxTeamAccounts)
            return new Error(ErrorCodes.TeamLimitReached, "All 5 team slots are used. Delete an account to free a slot.");
        if (await team.UsernameOrEmailTakenAsync(request.Username, request.Email.Trim(), ct)) return new Error(ErrorCodes.Conflict, "Username or email already in use.");
        var groups = await conversations.ListForStartupAsync(owner.StartupId.Value, ct);
        if (!ValidGroups(request, groups)) return new Error(ErrorCodes.Validation, "Choose groups belonging to your startup.");
        var generated = string.IsNullOrEmpty(request.Password) ? SecureTokens.NewPassword() : null;
        var t = new TeamAccount { StartupId = owner.StartupId.Value, PasswordHash = hasher.Hash(generated ?? request.Password!), Status = TeamAccountStatus.Active };
        SetDetails(t, request);
        await team.AddAsync(t, ct);
        try
        {
            await uow.SaveChangesAsync(ct);
            await ApplyGroupsAsync(t, groups, request.GroupIds ?? [], ct);
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException ex) { return Conflict(ex); }
        return Result.Success(new OneTimeCredential(t.Id, t.Username, generated), "Team account created.");
    }

    public async Task<Result> UpdateAsync(Actor owner, int id, CreateTeamAccountRequest request, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var member = await OwnedAsync(owner, id, ct);
        if (member is null) return Result.Failure(Guard.Forbidden());
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return Result.Failure(invalid);
        var groups = await conversations.ListForStartupAsync(member.StartupId, ct);
        if (!ValidGroups(request, groups)) return Result.Failure(ErrorCodes.Validation, "Choose groups belonging to your startup.");
        if (!string.IsNullOrEmpty(request.Password) || member.Username != request.Username || member.FullName != request.FullName.Trim())
            member.SessionVersion++;
        SetDetails(member, request);
        if (!string.IsNullOrEmpty(request.Password)) member.PasswordHash = hasher.Hash(request.Password);
        try
        {
            await ApplyGroupsAsync(member, groups, request.GroupIds ?? [], ct);
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException ex) { return Result.Failure(Conflict(ex)); }
        return Result.Success("Team member updated.");
    }

    /// <summary>FR-045: only the owner resets team passwords; team members have no self-service reset.</summary>
    public async Task<Result<OneTimeCredential>> ResetPasswordAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null) return Guard.Forbidden();
        var password = SecureTokens.NewPassword();
        t.PasswordHash = hasher.Hash(password);
        t.SessionVersion++;
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success(new OneTimeCredential(t.Id, t.Username, password));
    }

    public async Task<Result> DeactivateAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null || t.Status != TeamAccountStatus.Active) return Result.Failure(Guard.Forbidden());
        var groups = await conversations.ListForStartupAsync(t.StartupId, ct);
        var choices = GroupChoices(t.GroupMode, groups, groups.Where(c => HasMember(c, t.Id)).Select(c => c.Id).ToList());
        t.Status = TeamAccountStatus.Deactivated;
        t.SessionVersion++;
        await ApplyGroupsAsync(t, groups, choices, ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success($"{t.FullName} deactivated. Group access revoked.");
    }

    public async Task<Result> ReactivateAsync(Actor owner, int teamAccountId, bool restorePreviousConversations, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null || t.Status != TeamAccountStatus.Deactivated) return Result.Failure(Guard.Forbidden());
        var groups = await conversations.ListForStartupAsync(t.StartupId, ct);
        var choices = restorePreviousConversations ? SavedChoices(t) : [];
        if (!restorePreviousConversations) t.GroupMode = TeamGroupMode.Selected;
        t.Status = TeamAccountStatus.Active;
        t.PreviousConversationIds = "";
        await ApplyGroupsAsync(t, groups, choices, ct);
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success($"{t.FullName} reactivated.");
    }

    public async Task<Result> DeleteAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null) return Result.Failure(Guard.Forbidden());
        t.Status = TeamAccountStatus.Deleted;
        await ApplyGroupsAsync(t, await conversations.ListForStartupAsync(t.StartupId, ct), [], ct);
        t.PreviousConversationIds = "";
        t.PasswordHash = "";
        t.Username = $"deleted-{t.Id}"; t.Email = $"deleted-{t.Id}@invalid";
        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success("Team account deleted. One slot freed.");
    }
}
