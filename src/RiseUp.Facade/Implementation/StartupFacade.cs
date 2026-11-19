using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Startups;

public sealed class StartupFacade(IStartupRepository startups, IInvestorRepository investors, IUserAccountRepository accounts,
    IConnectionRepository connections, IVerificationRepository verification, IPromotionFacade promotions, IUnitOfWork uow, IClock clock,
    IValidator<UpdateStartupRequest> validator, ActivityRecorder activity) : IStartupFacade
{
    public async Task<PagedResult<StartupCardDto>> SearchAsync(Actor actor, SearchStartupsRequest request, CancellationToken ct = default)
    {
        var page = new PageRequest(request.Page, request.PageSize);
        var text = request.Text?.Trim();
        if (text?.Length > 100) text = text[..100];
        var (items, total) = await startups.SearchAsync(new StartupSearch(text, request.Industry, request.Stage, request.Location, request.Sort, page.Skip, page.SafeSize, false), ct);
        return new PagedResult<StartupCardDto>(items.Select(s => new StartupCardDto(s.Id, s.Name, s.Tagline, s.Description, s.Industry, s.Stage, s.Location, s.VerificationStatus, s.ViewCount, s.ColorIndex, s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId))).ToList(),
            total, page.SafePage, page.SafeSize);
    }

    public async Task<Result<StartupDetailDto>> GetDetailAsync(Actor actor, int id, string viewerKey, CancellationToken ct = default)
    {
        var s = await startups.GetWithMetricsAsync(id, ct);
        if (s is null || s.IsDeleted || s.IsSuspended || s.VerificationStatus is VerificationStatus.Unverified) return Guard.NotFound("Startup");

        // FR-120: at most one counted view per viewer per startup every 24 hours.
        var now = clock.UtcNow;
        if (!await startups.HasViewSinceAsync(s.Id, viewerKey, now - BusinessRules.ProfileViewDedupe, ct))
        {
            await startups.AddViewAsync(new ProfileViewEvent { StartupId = s.Id, ViewerKey = viewerKey, ViewedAtUtc = now }, ct);
            s.ViewCount++;
            await uow.SaveChangesAsync(ct);
        }

        var isOwner = actor.IsFounder && actor.StartupId == s.Id;
        var investor = actor.IsInvestor ? await investors.GetByIdAsync(actor.InvestorId!.Value, ct) : null;
        var verifiedInvestor = investor is { IsActiveVerified: true };
        var isConnected = investor is not null && await connections.GetConnectionAsync(s.Id, investor.Id, ct) is not null;
        var canSeeRestricted = isOwner || verifiedInvestor;       // FR-027
        var canSeeContact = isOwner || isConnected;               // FR-041
        var owner = canSeeContact ? await accounts.GetByIdAsync(s.OwnerAccountId, ct) : null;
        var canInvite = verifiedInvestor && s.IsActiveVerified && !isConnected && await connections.GetPendingForPairAsync(s.Id, investor!.Id, ct) is null;

        return Result.Success(new StartupDetailDto(s.Id, s.Name, s.Tagline, s.Description, s.Industry, s.Stage, s.Location, s.FoundingYear,
            s.BusinessModel, s.Website, s.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), s.VerificationStatus, s.VerifiedUntilUtc,
            s.ViewCount, s.ColorIndex,
            canSeeRestricted ? s.FundingGoalUsd : null,
            canSeeRestricted ? s.Metrics.Select(m => (m.Name, m.Value)).ToList() : null,
            owner?.FullName, owner?.Email, owner?.Phone, isOwner, isConnected, canInvite,
            s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId), s.CoverMediaId is null ? null : UrlRewrite.Media(s.CoverMediaId)));
    }

    /// <summary>FR-020: ordinary edits keep verification; a legal-name change triggers re-verification and restrictions.</summary>
    public async Task<Result> UpdateProfileAsync(Actor actor, UpdateStartupRequest request, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Result.Failure(Guard.Forbidden("Only the Main Startup Owner can edit the profile."));
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return Result.Failure(invalid);
        var s = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (s is null) return Result.Failure(Guard.NotFound("Startup"));
        var normalized = request.Name.Trim().ToUpperInvariant();
        var legalChange = normalized != s.NormalizedLegalName;
        if (legalChange && await startups.LegalNameExistsAsync(normalized, s.Id, ct))
            return Result.Failure(ErrorCodes.Conflict, "Another startup is registered with this legal name. It will be reviewed manually.");

        s.Tagline = request.Tagline.Trim(); s.Description = request.Description.Trim(); s.Industry = request.Industry.Trim(); s.Stage = request.Stage;
        s.Location = request.Location.Trim(); s.FoundingYear = request.FoundingYear; s.BusinessModel = request.BusinessModel?.Trim();
        s.Website = request.Website?.Trim(); s.Tags = request.Tags?.Trim() ?? ""; s.FundingGoalUsd = request.FundingGoalUsd;

        if (legalChange && s.VerificationStatus is VerificationStatus.Verified or VerificationStatus.Expired)
        {
            var old = s.Name;
            await promotions.PauseForStartupAsync(s.Id, "re-verification after legal change", ct);
            s.Name = request.Name.Trim(); s.NormalizedLegalName = normalized; s.VerificationStatus = VerificationStatus.Pending; s.VerifiedUntilUtc = null;
            await verification.AddAsync(new VerificationApplication { Subject = VerificationSubject.Startup, StartupId = s.Id, Status = VerificationStatus.Pending, SubmittedAtUtc = clock.UtcNow, LicenceHolderType = LicenceHolderType.Individual, LicenceHolderName = (await accounts.GetByIdAsync(s.OwnerAccountId, ct))?.FullName }, ct);
            await activity.AuditAsync(actor, "Legal name change → re-verification", $"startup:{s.Id}", $"{old} → {s.Name}", ct: ct);
            await uow.SaveChangesAsync(ct);
            return Result.Success("Name changed. Re-verification requested; pitching, posting and promotions pause until approval.");
        }
        if (legalChange) { s.Name = request.Name.Trim(); s.NormalizedLegalName = normalized; }
        await uow.SaveChangesAsync(ct);
        return Result.Success("Profile saved. Ordinary edits don't affect verification.");
    }

    /// <summary>FR-149–152: suspension cancels pending requests, disables chats, pauses promotions, preserves wallet/history.</summary>
    public async Task<Result> SuspendAsync(Actor moderator, int startupId, string reason, CancellationToken ct = default)
    {
        if (!moderator.IsAdmin(AdminRole.Moderator)) return Result.Failure(Guard.Forbidden());
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return Result.Failure(ErrorCodes.Validation, "A policy finding is required.");
        var s = await startups.GetByIdAsync(startupId, ct);
        if (s is null) return Result.Failure(Guard.NotFound("Startup"));
        await promotions.PauseForStartupAsync(s.Id, "account suspended", ct);
        s.IsSuspended = true;
        var cancelled = 0;
        foreach (var r in await connections.ListPendingForStartupAsync(s.Id, ct)) { r.Status = RequestStatus.Cancelled; r.DecidedAtUtc = clock.UtcNow; r.DecisionNote = "account suspended"; cancelled++; }
        await activity.AuditAsync(moderator, "Suspended startup account", $"startup:{s.Id}", reason.Trim(), ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success($"{s.Name} suspended: {cancelled} pending request(s) cancelled, chats disabled, promotions paused.");
    }

    public async Task<Result> LiftSuspensionAsync(Actor moderator, int startupId, string reason, CancellationToken ct = default)
    {
        if (!moderator.IsAdmin(AdminRole.Moderator)) return Result.Failure(Guard.Forbidden());
        var s = await startups.GetByIdAsync(startupId, ct);
        if (s is null) return Result.Failure(Guard.NotFound("Startup"));
        s.IsSuspended = false;
        if (s.VerificationStatus == VerificationStatus.Verified) await promotions.ResumeForStartupAsync(s.Id, ct);
        await activity.AuditAsync(moderator, "Lifted suspension", $"startup:{s.Id}", reason, ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Suspension lifted.");
    }
}
