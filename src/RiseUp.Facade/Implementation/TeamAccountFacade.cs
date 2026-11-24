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

public sealed class TeamAccountFacade(ITeamAccountRepository team, IConversationRepository conversations, IInvestorRepository investors,
    IPasswordHasher hasher, IUnitOfWork uow, IClock clock, IValidator<CreateTeamAccountRequest> validator, ActivityRecorder activity) : ITeamAccountFacade
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
        return (await team.ListForStartupAsync(owner.StartupId.Value, ct)).Select(t => new TeamAccountDto(t.Id, t.FullName, t.Designation, t.Username, t.Email, t.Status,
            convs.Where(c => c.Members.Any(m => m.TeamAccountId == t.Id && m.RemovedAtUtc == null)).Select(c => c.Id).ToList())).ToList();
    }

    /// <summary>FR-043: max five ACTIVE OR DEACTIVATED logins; deleted accounts free their slot.</summary>
    public async Task<Result<OneTimeCredential>> CreateAsync(Actor owner, CreateTeamAccountRequest request, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can create team accounts.");
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return invalid;
        if (await team.CountOccupyingSlotsAsync(owner.StartupId.Value, ct) >= BusinessRules.MaxTeamAccounts)
            return new Error(ErrorCodes.TeamLimitReached, "All 5 team slots are used (active and deactivated accounts both count). Delete an account to free a slot.");
        if (await team.UsernameOrEmailTakenAsync(request.Username, request.Email.Trim(), ct)) return new Error(ErrorCodes.Conflict, "Username or email already in use.");
        var password = SecureTokens.NewPassword();
        var t = new TeamAccount
        {
            StartupId = owner.StartupId.Value, FullName = request.FullName.Trim(), Designation = request.Designation.Trim(), Username = request.Username,
            Email = request.Email.Trim(), LinkedInUrl = request.LinkedInUrl, PasswordHash = hasher.Hash(password), Status = TeamAccountStatus.Active,
        };
        await team.AddAsync(t, ct);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException ex)
        {
            return ex.ContactError ?? new Error(ErrorCodes.Conflict, "Username or email already in use.");
        }
        return Result.Success(new OneTimeCredential(t.Id, t.Username, password), "Team account created. Copy the password now — it will not be shown again.");
    }

    /// <summary>FR-045: only the owner resets team passwords; team members have no self-service reset.</summary>
    public async Task<Result<OneTimeCredential>> ResetPasswordAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null) return Guard.Forbidden();
        var password = SecureTokens.NewPassword();
        t.PasswordHash = hasher.Hash(password);
        await uow.SaveChangesAsync(ct);
        return Result.Success(new OneTimeCredential(t.Id, t.Username, password));
    }

    public async Task<Result> DeactivateAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null) return Result.Failure(Guard.Forbidden());
        var now = clock.UtcNow;
        var held = new List<int>();
        foreach (var c in await conversations.ListForStartupAsync(t.StartupId, ct))
        {
            var tracked = await conversations.GetWithMembersAsync(c.Id, ct);
            foreach (var m in tracked!.Members.Where(m => m.TeamAccountId == t.Id && m.RemovedAtUtc == null)) { m.RemovedAtUtc = now; held.Add(c.Id); }
        }
        t.PreviousConversationIds = string.Join(',', held.Distinct());
        t.Status = TeamAccountStatus.Deactivated;
        await uow.SaveChangesAsync(ct);
        return Result.Success($"{t.FullName} deactivated. Access revoked immediately; the slot stays occupied.");
    }

    /// <summary>FR-054: owner chooses to restore previous groups or start fresh.</summary>
    public async Task<Result> ReactivateAsync(Actor owner, int teamAccountId, bool restorePreviousConversations, CancellationToken ct = default)
    {
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null || t.Status != TeamAccountStatus.Deactivated) return Result.Failure(Guard.Forbidden());
        t.Status = TeamAccountStatus.Active;
        if (restorePreviousConversations)
        {
            var now = clock.UtcNow;
            foreach (var id in t.PreviousConversationIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse))
            {
                var c = await conversations.GetWithMembersAsync(id, ct);
                if (c is null || c.StartupId != t.StartupId) continue;
                await conversations.AddMemberAsync(new ConversationMember { ConversationId = c.Id, Kind = MemberKind.Team, TeamAccountId = t.Id, AddedAtUtc = now }, ct);
                await activity.NotifyAccountAsync((await investors.GetByIdAsync(c.InvestorId, ct))?.AccountId, $"{t.FullName} rejoined your conversation.", $"/conversations/{c.Id}", ct);
            }
        }
        await uow.SaveChangesAsync(ct);
        return Result.Success($"{t.FullName} reactivated.");
    }

    public async Task<Result> DeleteAsync(Actor owner, int teamAccountId, CancellationToken ct = default)
    {
        var t = await OwnedAsync(owner, teamAccountId, ct);
        if (t is null) return Result.Failure(Guard.Forbidden());
        if (t.Status == TeamAccountStatus.Active) await DeactivateAsync(owner, teamAccountId, ct);
        t.Status = TeamAccountStatus.Deleted;
        t.PasswordHash = "";
        t.Username = $"deleted-{t.Id}"; t.Email = $"deleted-{t.Id}@invalid";
        await uow.SaveChangesAsync(ct);
        return Result.Success("Team account deleted. One slot freed.");
    }
}
