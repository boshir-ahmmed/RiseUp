using System.Text.RegularExpressions;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Profiles;
using RiseUp.Facade.Common;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Profiles;


/// <summary>
/// Personal profiles. Team members can change their photo only (name/title are managed by the Main Startup Owner).
/// Display names: 1 change per 30 days, impersonation words blocked; on a VERIFIED founder/investor account the change is
/// reviewed by a Verification Administrator against the ID on file and the old name stays live until approval.
/// </summary>
public sealed partial class ProfileFacade(IProfileRepository profiles, IUserAccountRepository accounts, IInvestorRepository investors,
    IStartupRepository startups, ITeamAccountRepository team, IConnectionRepository connections, IWalletRepository wallets, IGovernanceRepository governance,
    MediaIntake intake, IUnitOfWork uow, IClock clock, ActivityRecorder activity) : IProfileFacade
{
    [GeneratedRegex(@"^\p{L}[\p{L}\p{M} .'’\-]*$")] private static partial Regex NamePattern();
    [GeneratedRegex(@"^https?://[^\s/$.?#][^\s]*\.[^\s]+$", RegexOptions.IgnoreCase)] private static partial Regex LinkPattern();

    // ------------------------------------------------------------------ loading
    private async Task<MemberProfile?> LoadAsync(Actor actor, bool create, CancellationToken ct)
    {
        MemberProfile? p = actor.IsTeamMember ? await profiles.GetForTeamAsync(actor.TeamAccountId!.Value, ct)
            : actor.AccountId is int a ? await profiles.GetForAccountAsync(a, ct) : null;
        if (p is null && create && actor.IsAuthenticated)
        {
            p = new MemberProfile { AccountId = actor.IsTeamMember ? null : actor.AccountId, TeamAccountId = actor.IsTeamMember ? actor.TeamAccountId : null };
            await profiles.AddAsync(p, ct);
        }
        return p;
    }

    private async Task<(string Name, string DefaultHeadline, int Color, bool Verified, string RoleLine, string? StartupName)> IdentityAsync(Actor actor, CancellationToken ct)
    {
        if (actor.IsInvestor && await investors.GetByIdAsync(actor.InvestorId!.Value, ct) is { } i)
            return (i.DisplayName, $"{(i.InvestorType == "Venture fund" ? "Partner" : i.InvestorType)} · {i.Affiliation}", i.ColorIndex, i.IsActiveVerifiedAt(clock.UtcNow), i.Affiliation, null);
        if (actor.IsTeamMember && await team.GetByIdAsync(actor.TeamAccountId!.Value, ct) is { } t)
        {
            var s = await startups.GetByIdAsync(t.StartupId, ct);
            return (t.FullName, $"{t.Designation} · {s?.Name}", 3, false, $"Team member at {s?.Name}", s?.Name);
        }
        var account = actor.AccountId is int a ? await accounts.GetByIdAsync(a, ct) : null;
        if (actor.IsFounder && actor.StartupId is int sid && await startups.GetByIdAsync(sid, ct) is { } st)
            return (account?.FullName ?? actor.Name, $"Founder · {st.Name}", st.ColorIndex, st.IsActiveVerifiedAt(clock.UtcNow), $"Founder of {st.Name}", st.Name);
        return (account?.FullName ?? actor.Name, $"{actor.AdminRole} · RiseUp", 4, false, "RiseUp administration", null);
    }

    private static bool CanEditDetails(Actor actor) => actor.IsAuthenticated && !actor.IsTeamMember;
    private static bool CanHaveCover(Actor actor) => actor.IsFounder || actor.IsInvestor;

    public async Task<ShellDto> GetShellAsync(Actor actor, CancellationToken ct = default)
    {
        var id = await IdentityAsync(actor, ct);
        var p = await LoadAsync(actor, false, ct);
        var unread = (await governance.ListNotificationsAsync(actor.AccountId, actor.TeamAccountId, 100, ct)).Count(n => !n.IsRead);
        var stats = new List<ShellStat>(); var badge = 0;
        if (actor.IsFounder && actor.StartupId is int sid && await startups.GetByIdAsync(sid, ct) is { } s)
        {
            var w = await wallets.GetByStartupAsync(sid, ct);
            stats.Add(new("Profile views", s.ViewCount.ToString("N0"), "/settings"));
            stats.Add(new("Credits", ((w?.WelcomeBalance ?? 0) + (w?.PurchasedBalance ?? 0)).ToString(), "/wallet"));
            badge = (await connections.ListPendingForStartupAsync(sid, ct)).Count(r => r.Type == RequestType.Invitation);
        }
        if (actor.IsInvestor)
        {
            badge = (await connections.ListPendingForInvestorAsync(actor.InvestorId!.Value, ct)).Count(r => r.Type == RequestType.Pitch);
            stats.Add(new("Pending pitches", badge.ToString(), "/requests"));
        }
        var pending = actor.AccountId is int aid && await profiles.GetPendingNameRequestAsync(aid, ct) is not null;
        return new ShellDto(id.Name, string.IsNullOrWhiteSpace(p?.Headline) ? id.DefaultHeadline : p!.Headline, UrlRewrite.Media(p?.PhotoMediaId) is { Length: > 0 } u ? u : null,
            UrlRewrite.Media(p?.CoverMediaId) is { Length: > 0 } c ? c : null, id.Color, id.Verified, pending, unread, badge, stats);
    }

    public async Task<Result<MyProfileDto>> GetMineAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsAuthenticated) return new Error(ErrorCodes.Unauthenticated, "Please sign in.");
        var id = await IdentityAsync(actor, ct);
        var p = await LoadAsync(actor, false, ct) ?? new MemberProfile();
        var pending = actor.AccountId is int aid && !actor.IsTeamMember ? await profiles.GetPendingNameRequestAsync(aid, ct) : null;
        DateTime? next = p.NameChangedAtUtc is DateTime changed && changed + BusinessRules.NameChangeCooldown > clock.UtcNow ? changed + BusinessRules.NameChangeCooldown : null;
        var headline = string.IsNullOrWhiteSpace(p.Headline) ? id.DefaultHeadline : p.Headline;
        List<(string, bool)> checklist = actor.IsTeamMember ? [("Profile photo", p.PhotoMediaId is not null)] :
        [
            ("Profile photo", p.PhotoMediaId is not null), ("Cover photo", p.CoverMediaId is not null || !CanHaveCover(actor)), ("Headline", headline.Length >= 10),
            ("Location", p.Location.Length > 0), ("Bio (80+ characters)", p.Bio.Length >= 80), ("Website or LinkedIn", !string.IsNullOrEmpty(p.LinkUrl)),
        ];
        return Result.Success(new MyProfileDto(id.Name, headline, p.Location, p.Bio, p.LinkUrl, p.PhotoMediaId is null ? null : UrlRewrite.Media(p.PhotoMediaId),
            p.CoverMediaId is null ? null : UrlRewrite.Media(p.CoverMediaId), id.Color, id.Verified, actor.IsTeamMember, actor.IsAnyAdmin,
            CanEditDetails(actor) && pending is null && next is null, CanHaveCover(actor), pending?.ToName, next, id.RoleLine,
            actor.IsTeamMember ? (await team.GetByIdAsync(actor.TeamAccountId!.Value, ct))?.StartupId : actor.StartupId, id.StartupName, actor.InvestorId,
            (int)Math.Round(checklist.Count(c => c.Item2) * 100.0 / checklist.Count), checklist));
    }

    // ------------------------------------------------------------------ details + name rules
    public static string? ValidateName(string name)
    {
        if (name.Length < BusinessRules.DisplayNameMin || name.Length > BusinessRules.DisplayNameMax) return $"Use {BusinessRules.DisplayNameMin}–{BusinessRules.DisplayNameMax} characters.";
        if (!NamePattern().IsMatch(name)) return "Use letters, spaces, dots, apostrophes and hyphens only.";
        var words = name.ToLowerInvariant().Split([' ', '.', '-', '\'', '’'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => BusinessRules.ReservedNameWords.Contains(w))) return "Names can't include words like Admin, Official, RiseUp, Verified or Support.";
        return null;
    }

    public async Task<Result> UpdateAsync(Actor actor, UpdateProfileRequest request, CancellationToken ct = default)
    {
        if (!CanEditDetails(actor)) return Result.Failure(Guard.Forbidden("Team members can change their profile photo only. Ask your Main Startup Owner to update your name or title."));
        var headline = (request.Headline ?? "").Trim();
        var location = (request.Location ?? "").Trim();
        var bio = (request.Bio ?? "").Trim();
        var link = string.IsNullOrWhiteSpace(request.Link) ? null : request.Link.Trim();
        if (headline.Length is < 2 or > BusinessRules.HeadlineMax) return Result.Failure(ErrorCodes.Validation, $"Add a headline (2–{BusinessRules.HeadlineMax} characters).");
        if (location.Length > BusinessRules.ProfileLocationMax) return Result.Failure(ErrorCodes.Validation, $"Location is limited to {BusinessRules.ProfileLocationMax} characters.");
        if (bio.Length > BusinessRules.ProfileBioMax) return Result.Failure(ErrorCodes.Validation, $"About is limited to {BusinessRules.ProfileBioMax} characters.");
        if (link is not null && (link.Length > BusinessRules.ProfileLinkMax || !LinkPattern().IsMatch(link))) return Result.Failure(ErrorCodes.Validation, "Enter a full link starting with https://");

        var p = (await LoadAsync(actor, true, ct))!;
        var id = await IdentityAsync(actor, ct);
        var newName = Regex.Replace((request.Name ?? "").Trim(), @"\s+", " ");
        var message = "Profile saved.";
        if (newName.Length > 0 && newName != id.Name)
        {
            if (ValidateName(newName) is { } invalid) return Result.Failure(ErrorCodes.Validation, invalid);
            if (p.NameChangedAtUtc is DateTime last && clock.UtcNow - last < BusinessRules.NameChangeCooldown)
                return Result.Failure(ErrorCodes.NameChangeCooldown, $"You can change your name again on {DhakaTime.ToDhaka(last + BusinessRules.NameChangeCooldown):dd MMM yyyy}.");
            if (await profiles.GetPendingNameRequestAsync(actor.AccountId!.Value, ct) is not null)
                return Result.Failure(ErrorCodes.NameChangePending, "A name change is already waiting for review. Cancel it first to request a different name.");
            if (id.Verified)
            {
                await profiles.AddNameRequestAsync(new NameChangeRequest { AccountId = actor.AccountId!.Value, FromName = id.Name, ToName = newName, Status = NameChangeStatus.Pending }, ct);
                await activity.AuditAsync(actor, "Name change requested", $"account:{actor.AccountId}", $"{id.Name} → {newName}", ct: ct);
                message = "Saved. Your name change was sent for review — your current name stays until a Verification Administrator approves it.";
            }
            else
            {
                await ApplyNameAsync(actor.AccountId!.Value, actor.IsInvestor ? actor.InvestorId : null, newName, p, ct);
                await activity.AuditAsync(actor, "Name changed", $"account:{actor.AccountId}", $"{id.Name} → {newName}", ct: ct);
                message = "Profile saved with your new name.";
            }
        }
        p.Headline = headline; p.Location = location; p.Bio = bio; p.LinkUrl = link;
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Failure(ErrorCodes.NameChangePending, "A name change is already waiting for review."); }
        return Result.Success(message);
    }

    /// <summary>Updates the identity record(s) and every stored author-name copy so the new name shows everywhere.</summary>
    private async Task ApplyNameAsync(int accountId, int? investorId, string newName, MemberProfile profile, CancellationToken ct)
    {
        if (await accounts.GetByIdAsync(accountId, ct) is { } account) account.FullName = newName;
        investorId ??= (await investors.GetByAccountAsync(accountId, ct))?.Id;
        if (investorId is int iid && await investors.GetByIdAsync(iid, ct) is { } inv) inv.DisplayName = newName;
        var (comments, messages, questions, meetings) = await profiles.LoadAuthoredAsync(accountId, null, ct);
        foreach (var c in comments) c.AuthorName = newName;
        foreach (var m in messages) m.AuthorName = newName;
        foreach (var q in questions) q.AskerName = newName;
        foreach (var m in meetings) m.ProposedByName = newName;
        profile.NameChangedAtUtc = clock.UtcNow;
    }

    public async Task<Result> CancelNameChangeAsync(Actor actor, CancellationToken ct = default)
    {
        if (actor.AccountId is not int a || await profiles.GetPendingNameRequestAsync(a, ct) is not { } r) return Result.Failure(Guard.NotFound("Pending name change"));
        r.Status = NameChangeStatus.Cancelled; r.DecidedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Name change request cancelled.");
    }

    private static bool CanReviewNames(Actor admin) => admin.IsAdmin(AdminRole.VerificationAdmin) || admin.IsAdmin(AdminRole.SuperAdmin);

    public async Task<IReadOnlyList<NameChangeDto>> ListNameChangesAsync(Actor admin, CancellationToken ct = default)
    {
        if (!CanReviewNames(admin)) return [];
        var list = await profiles.ListNameRequestsAsync(ct);
        var result = new List<NameChangeDto>();
        foreach (var r in list)
        {
            var account = await accounts.GetByIdAsync(r.AccountId, ct);
            var label = account?.PrimaryRole == PrimaryRole.Investor ? "Investor" : account?.PrimaryRole == PrimaryRole.Founder
                ? $"Founder · {(await startups.GetByOwnerAsync(r.AccountId, ct))?.Name}" : "Account";
            result.Add(new NameChangeDto(r.Id, r.AccountId, $"{label} · {account?.Email}", r.FromName, r.ToName, r.Status, r.CreatedAtUtc, r.Reason));
        }
        return result;
    }

    public async Task<Result> DecideNameChangeAsync(Actor admin, int requestId, bool approve, string? reason, CancellationToken ct = default)
    {
        if (!CanReviewNames(admin)) return Result.Failure(Guard.Forbidden("Only Verification Administrators can review name changes."));
        var r = await profiles.GetNameRequestAsync(requestId, ct);
        if (r is null) return Result.Failure(Guard.NotFound("Name change request"));
        if (r.Status != NameChangeStatus.Pending) return Result.Failure(ErrorCodes.NotPending, "This request has already been decided.");
        if (!approve && (reason?.Trim().Length ?? 0) < 5) return Result.Failure(ErrorCodes.Validation, "A rejection reason of at least 5 characters is required.");
        r.Status = approve ? NameChangeStatus.Approved : NameChangeStatus.Rejected;
        r.ReviewerAccountId = admin.AccountId; r.DecidedAtUtc = clock.UtcNow; r.Reason = reason?.Trim();
        if (approve)
        {
            var profile = await profiles.GetForAccountAsync(r.AccountId, ct);
            if (profile is null) { profile = new MemberProfile { AccountId = r.AccountId }; await profiles.AddAsync(profile, ct); }
            await ApplyNameAsync(r.AccountId, null, r.ToName, profile, ct);
            await activity.NotifyAccountAsync(r.AccountId, $"Your name change to “{r.ToName}” was approved.", "/me", ct);
        }
        else await activity.NotifyAccountAsync(r.AccountId, $"Your name change to “{r.ToName}” was not approved: {r.Reason}", "/me", ct);
        await activity.AuditAsync(admin, approve ? "Name change approved" : "Name change rejected", $"account:{r.AccountId}", $"{r.FromName} → {r.ToName}. {r.Reason}", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(approve ? "Approved — the new name is live everywhere." : "Rejected. The user keeps their current name.");
    }

    // ------------------------------------------------------------------ images
    public async Task<Result> SetPhotoAsync(Actor actor, UploadedFile file, CancellationToken ct = default)
    {
        if (!actor.IsAuthenticated) return Result.Failure(ErrorCodes.Unauthenticated, "Please sign in.");
        var accepted = await intake.AcceptAsync(MediaKind.ProfilePhoto, file, null, actor.IsTeamMember ? null : actor.AccountId, actor.TeamAccountId, ct);
        if (accepted.IsFailure) return accepted;
        var p = (await LoadAsync(actor, true, ct))!;
        var old = p.PhotoMediaId;
        await uow.SaveChangesAsync(ct);   // assigns the media id
        p.PhotoMediaId = accepted.Value!.Id;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await activity.AuditAsync(actor, "Profile photo updated", actor.ViewerKey, "", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(accepted.Value.ScanStatus == ScanStatus.Clean ? "Profile photo updated." : "Photo uploaded. It will appear once the security scan finishes.");
    }

    public async Task<Result> RemovePhotoAsync(Actor actor, CancellationToken ct = default)
    {
        var p = await LoadAsync(actor, false, ct);
        if (p?.PhotoMediaId is null) return Result.Success("No photo to remove.");
        var old = p.PhotoMediaId; p.PhotoMediaId = null;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Photo removed — your initials are shown instead.");
    }

    public async Task<Result> SetCoverAsync(Actor actor, UploadedFile file, CancellationToken ct = default)
    {
        if (!CanHaveCover(actor)) return Result.Failure(Guard.Forbidden("Cover photos are available to founders and investors."));
        var accepted = await intake.AcceptAsync(MediaKind.ProfileCover, file, null, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        var p = (await LoadAsync(actor, true, ct))!;
        var old = p.CoverMediaId;
        await uow.SaveChangesAsync(ct);
        p.CoverMediaId = accepted.Value!.Id;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Cover photo updated.");
    }

    public async Task<Result> RemoveCoverAsync(Actor actor, CancellationToken ct = default)
    {
        var p = await LoadAsync(actor, false, ct);
        if (p?.CoverMediaId is null) return Result.Success("No cover to remove.");
        var old = p.CoverMediaId; p.CoverMediaId = null;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Cover photo removed.");
    }

    public async Task<Result> SetStartupImageAsync(Actor actor, MediaKind kind, UploadedFile file, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Result.Failure(Guard.Forbidden("Only the Main Startup Owner can change startup branding."));
        if (kind is not (MediaKind.StartupLogo or MediaKind.StartupCover)) return Result.Failure(ErrorCodes.Validation, "Unknown image type.");
        var s = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (s is null) return Result.Failure(Guard.NotFound("Startup"));
        var accepted = await intake.AcceptAsync(kind, file, s.Id, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        await uow.SaveChangesAsync(ct);
        var old = kind == MediaKind.StartupLogo ? s.LogoMediaId : s.CoverMediaId;
        if (kind == MediaKind.StartupLogo) s.LogoMediaId = accepted.Value!.Id; else s.CoverMediaId = accepted.Value!.Id;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await activity.AuditAsync(actor, kind == MediaKind.StartupLogo ? "Startup logo updated" : "Startup cover updated", $"startup:{s.Id}", "", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(kind == MediaKind.StartupLogo ? "Logo updated everywhere on RiseUp." : "Startup cover updated.");
    }

    public async Task<Result> RemoveStartupImageAsync(Actor actor, MediaKind kind, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Result.Failure(Guard.Forbidden());
        var s = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (s is null) return Result.Failure(Guard.NotFound("Startup"));
        var old = kind == MediaKind.StartupLogo ? s.LogoMediaId : s.CoverMediaId;
        if (kind == MediaKind.StartupLogo) s.LogoMediaId = null; else s.CoverMediaId = null;
        await intake.DiscardAsync(old, clock.UtcNow, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Image removed.");
    }
}
