using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Verification;

public sealed partial class VerificationFacade(IVerificationRepository applications, IStartupRepository startups, IInvestorRepository investors,
    IUserAccountRepository accounts, IWalletRepository wallets, IConnectionRepository connections, IPromotionFacade promotions,
    IUnitOfWork uow, IClock clock, ActivityRecorder activity, IFileStorage storage, IMalwareScanner scanner, IVerificationUploadStorage intake,
    Microsoft.Extensions.Logging.ILogger<VerificationFacade> logger) : IVerificationFacade
{
    private static bool CanReview(Actor a) => a.AccountId.HasValue && (a.IsAdmin(AdminRole.VerificationAdmin) || a.IsAdmin(AdminRole.SuperAdmin));

    public async Task<Result<int>> SubmitAsync(Actor actor, SubmitVerificationRequest request, CancellationToken ct = default)
    {
        VerificationSubject subject;
        int subjectId;
        if (actor.IsFounder && actor.StartupId is int sid)
        {
            subject = VerificationSubject.Startup;
            subjectId = sid;
            if (request.LicenceHolderType is not (LicenceHolderType.Company or LicenceHolderType.Individual) ||
                string.IsNullOrWhiteSpace(request.LicenceHolderName) || request.LicenceHolderName.Trim().Length > 150)
                return new Error(ErrorCodes.Validation, "State whether the trade licence names an individual or a company, and the holder's name.");
        }
        else if (actor.IsInvestor && actor.InvestorId is int iid)
        {
            subject = VerificationSubject.Investor;
            subjectId = iid;
        }
        else return Guard.Forbidden();

        if (!HasRequiredEvidence(subject, request.LicenceHolderType, request.Documents.Select(d => d.Kind)))
            return new Error(ErrorCodes.Validation, "Supply an identity document and the required business, ownership or investor evidence.");

        var startup = subject == VerificationSubject.Startup ? await startups.GetByIdAsync(subjectId, ct) : null;
        var investor = subject == VerificationSubject.Investor ? await investors.GetByIdAsync(subjectId, ct) : null;
        if (actor.AccountId is null || (subject == VerificationSubject.Startup
            ? startup is null || startup.IsDeleted || startup.OwnerAccountId != actor.AccountId
            : investor is null || investor.IsDeleted || investor.AccountId != actor.AccountId))
            return Guard.Forbidden();

        var pending = await applications.GetPendingForAsync(subject, subjectId, ct);
        if (pending?.Documents.Count > 0) return new Error(ErrorCodes.Conflict, "An application is already awaiting review.");
        var app = pending ?? new VerificationApplication { Subject = subject, Status = VerificationStatus.Pending };
        if (startup != null)
        {
            app.StartupId = startup.Id;
            app.LicenceHolderName = request.LicenceHolderName!.Trim();
            app.LicenceHolderType = request.LicenceHolderType;
            if (startup.VerificationStatus is VerificationStatus.Unverified or VerificationStatus.Rejected) startup.VerificationStatus = VerificationStatus.Pending;
            startup.Version = Guid.NewGuid(); // serialize submissions and deletion even on renewal
        }
        else
        {
            app.InvestorId = investor!.Id;
            if (investor.VerificationStatus is VerificationStatus.Unverified or VerificationStatus.Rejected) investor.VerificationStatus = VerificationStatus.Pending;
            investor.Version = Guid.NewGuid();
        }
        app.Documents = request.Documents.Select(d => new VerificationDocument { Kind = d.Kind, StorageKey = d.StorageKey, ScanStatus = ScanStatus.Pending }).ToList();
        app.SubmittedAtUtc = clock.UtcNow;
        if (pending is null) await applications.AddAsync(app, ct);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return new Error(ErrorCodes.Conflict, "The applicant or application changed. Refresh before submitting again."); }
        return Result.Success(app.Id, "Submitted for manual review.");
    }

    public async Task<IReadOnlyList<ApplicationDto>> ListQueueAsync(Actor admin, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return [];
        var list = await applications.ListAsync(null, ct);
        var result = new List<ApplicationDto>();
        foreach (var a in list)
        {
            string name; string? owner = null;
            if (a.Subject == VerificationSubject.Startup)
            {
                var s = await startups.GetByIdAsync(a.StartupId!.Value, ct);
                name = s?.Name ?? "?"; owner = s is null ? null : (await accounts.GetByIdAsync(s.OwnerAccountId, ct))?.FullName;
            }
            else name = (await investors.GetByIdAsync(a.InvestorId!.Value, ct))?.DisplayName ?? "?";
            result.Add(new ApplicationDto(a.Id, a.Subject, a.StartupId ?? a.InvestorId ?? 0, name, owner, a.Status, a.LicenceHolderType, a.LicenceHolderName,
                a.SubmittedAtUtc, a.DecisionReason, a.Documents.Select(d => new EvidenceDto(d.Id, d.Kind, d.ScanStatus, d.Retention, d.Erased)).ToList()));
        }
        return result;
    }

    public async Task<Result> ApproveAsync(Actor admin, int applicationId, string note, DateOnly? earliestEvidenceExpiry = null, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return Result.Failure(Guard.Forbidden("Only a Verification Administrator can decide verification."));
        var app = await applications.GetByIdAsync(applicationId, ct);
        if (app is null || app.Status != VerificationStatus.Pending) return Result.Failure(ErrorCodes.NotPending, "This application is not pending.");
        var now = clock.UtcNow;
        var validUntil = now.Add(BusinessRules.VerificationValidity);
        if (earliestEvidenceExpiry is DateOnly date)
        {
            if (date < DhakaTime.ToDhakaDate(now)) return Result.Failure(ErrorCodes.Validation, "An evidence document has already expired.");
            if (date < DhakaTime.ToDhakaDate(validUntil))
                validUntil = DhakaTime.FromDhaka(date.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }
        var message = "Verified.";
        if (string.IsNullOrWhiteSpace(note) || note.Trim().Length is < 5 or > 500)
            return Result.Failure(ErrorCodes.Validation, "Enter a decision note of 5 to 500 characters.");
        if (!await IsSubjectActiveAsync(app, ct)) return Result.Failure(Guard.NotFound("Applicant"));
        if (!HasRequiredEvidence(app.Subject, app.LicenceHolderType, app.Documents.Select(d => d.Kind)))
            return Result.Failure(ErrorCodes.Validation, "Required evidence is missing or unclassified. Request a corrected application.");
        foreach (var d in app.Documents)
            if (d.ScanStatus != ScanStatus.Clean || d.Erased || d.Retention == EvidenceRetention.Erasing ||
                !await applications.WasOpenedByAsync(d.Id, admin.AccountId!.Value, ct))
                return Result.Failure(ErrorCodes.UploadPendingScan, "Open and review each clean document before approving.");

        if (app.Subject == VerificationSubject.Startup)
        {
            var startup = await startups.GetByIdAsync(app.StartupId!.Value, ct);
            var owner = await accounts.GetByIdAsync(startup!.OwnerAccountId, ct);
            // FR-013: an individually-named licence must name the Main Startup Owner.
            if (app.LicenceHolderType == LicenceHolderType.Individual &&
                !string.Equals(app.LicenceHolderName?.Trim(), owner?.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                return Result.Failure(ErrorCodes.OwnershipMismatch, "An individually named trade licence must belong to the Main Startup Owner.");
            startup.VerificationStatus = VerificationStatus.Verified;
            startup.VerifiedUntilUtc = validUntil;
            // FR-121/122: exactly 20 Welcome credits on FIRST verification only; idempotency key prevents duplicates.
            var key = $"welcome:{startup.Id}";
            if (!startup.WelcomeTokensGranted && !await wallets.LedgerKeyExistsAsync(key, ct))
            {
                var wallet = await wallets.GetByStartupAsync(startup.Id, ct);
                if (wallet is null) { wallet = new StartupWallet { StartupId = startup.Id }; await wallets.AddAsync(wallet, ct); }
                wallet.WelcomeBalance += BusinessRules.WelcomeTokens;
                await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = startup.Id, Description = "Welcome grant (first verification)", Bucket = TokenBucket.Welcome, Amount = BusinessRules.WelcomeTokens, Reference = $"verification:{app.Id}", IdempotencyKey = key }, ct);
                startup.WelcomeTokensGranted = true;
                message = $"{startup.Name} verified — {BusinessRules.WelcomeTokens} welcome credits added (one-time).";
            }
            else message = $"{startup.Name} re-verified — no additional welcome credits.";
            await promotions.ResumeForStartupAsync(startup.Id, ct);
            await activity.NotifyAccountAsync(startup.OwnerAccountId, message, "/dashboard", ct);
        }
        else
        {
            var investor = await investors.GetByIdAsync(app.InvestorId!.Value, ct);
            investor!.VerificationStatus = VerificationStatus.Verified;
            investor.VerifiedUntilUtc = validUntil;
            await activity.NotifyAccountAsync(investor.AccountId, "Your investor profile is verified.", "/dashboard", ct);
            message = $"{investor.DisplayName} verified.";
        }
        app.Status = VerificationStatus.Verified; app.DecidedAtUtc = now; app.ReviewerAccountId = admin.AccountId; app.DecisionReason = note;
        await activity.AuditAsync(admin, $"Approved {app.Subject} verification", $"application:{app.Id}", note, ct: ct);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Failure(ErrorCodes.Conflict, "This application changed. Refresh the review queue."); }
        return Result.Success(message);
    }

    public async Task<Result> RejectAsync(Actor admin, int applicationId, string reason, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return Result.Failure(Guard.Forbidden());
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 500) return Result.Failure(ErrorCodes.Validation, "A clear rejection reason is required (FR-019).");
        var app = await applications.GetByIdAsync(applicationId, ct);
        if (app is null || app.Status != VerificationStatus.Pending) return Result.Failure(ErrorCodes.NotPending, "This application is not pending.");
        app.Status = VerificationStatus.Rejected; app.DecidedAtUtc = clock.UtcNow; app.ReviewerAccountId = admin.AccountId; app.DecisionReason = reason.Trim();
        if (app.Subject == VerificationSubject.Startup)
        {
            var s = await startups.GetByIdAsync(app.StartupId!.Value, ct);
            if (s!.VerificationStatus == VerificationStatus.Pending) s.VerificationStatus = VerificationStatus.Rejected;
            await activity.NotifyAccountAsync(s.OwnerAccountId, $"Verification rejected: {reason.Trim()} You can correct and resubmit.", "/verification", ct);
        }
        else
        {
            var i = await investors.GetByIdAsync(app.InvestorId!.Value, ct);
            if (i!.VerificationStatus == VerificationStatus.Pending) i.VerificationStatus = VerificationStatus.Rejected;
            await activity.NotifyAccountAsync(i.AccountId, $"Verification rejected: {reason.Trim()} You can correct and resubmit.", "/verification", ct);
        }
        await activity.AuditAsync(admin, $"Rejected {app.Subject} verification", $"application:{app.Id}", reason.Trim(), ct: ct);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Failure(ErrorCodes.Conflict, "This application changed. Refresh the review queue."); }
        return Result.Success("Rejected with reason. The applicant can resubmit.");
    }

    /// <summary>FR-145–148: ordinary lapse cancels pending requests (no pitch refunds), pauses promotions, keeps chats.</summary>
    public async Task<Result> ExpireAsync(Actor admin, VerificationSubject subject, int subjectId, string reason, CancellationToken ct = default)
    {
        if (!CanReview(admin) && admin != Actor.System) return Result.Failure(Guard.Forbidden());
        var now = clock.UtcNow;
        var cancelled = 0;
        if (subject == VerificationSubject.Startup)
        {
            var s = await startups.GetByIdAsync(subjectId, ct);
            if (s is null) return Result.Failure(Guard.NotFound("Startup"));
            if (admin == Actor.System && (s.VerificationStatus != VerificationStatus.Verified || s.VerifiedUntilUtc > now)) return Result.Success();
            await promotions.PauseForStartupAsync(s.Id, "verification lapsed", ct);
            s.VerificationStatus = VerificationStatus.Expired;
            foreach (var r in await connections.ListPendingForStartupAsync(s.Id, ct)) { r.Status = RequestStatus.Cancelled; r.DecidedAtUtc = now; r.DecisionNote = "verification expired"; cancelled++; }
        }
        else
        {
            var i = await investors.GetByIdAsync(subjectId, ct);
            if (i is null) return Result.Failure(Guard.NotFound("Investor"));
            if (admin == Actor.System && (i.VerificationStatus != VerificationStatus.Verified || i.VerifiedUntilUtc > now)) return Result.Success();
            i.VerificationStatus = VerificationStatus.Expired;
            foreach (var r in await connections.ListPendingForInvestorAsync(i.Id, ct)) { r.Status = RequestStatus.Cancelled; r.DecidedAtUtc = now; r.DecisionNote = "verification expired"; cancelled++; }
        }
        await activity.AuditAsync(admin, "Verification expired", $"{subject}:{subjectId}", reason, ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success($"Verification expired. {cancelled} pending request(s) cancelled without pitch refunds.");
    }

    public async Task<Result<int>> RequestOwnershipTransferAsync(Actor owner, string incomingName, string incomingEmail, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return Guard.Forbidden("Only the current Main Startup Owner can start a transfer.");
        if (string.IsNullOrWhiteSpace(incomingName) || string.IsNullOrWhiteSpace(incomingEmail) || !incomingEmail.Contains('@'))
            return new Error(ErrorCodes.Validation, "Enter the incoming owner's legal name and email.");
        if (await applications.GetOpenTransferForStartupAsync(owner.StartupId.Value, ct) is not null) return new Error(ErrorCodes.Conflict, "A transfer is already in progress.");
        var transfer = new OwnershipTransferCase
        {
            StartupId = owner.StartupId.Value, FromAccountId = owner.AccountId!.Value, IncomingOwnerName = incomingName.Trim(), IncomingOwnerEmail = incomingEmail.Trim(),
            OwnerConsentAtUtc = clock.UtcNow, Status = TransferStatus.AwaitingIncomingConsent,
        };
        await applications.AddTransferAsync(transfer, ct);
        await activity.AuditAsync(owner, "Ownership transfer requested", $"startup:{owner.StartupId}", "current owner consent recorded", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(transfer.Id, "Your consent is recorded. The incoming owner must consent before review.");
    }

    public async Task<Result> RecordIncomingConsentAsync(int transferId, string incomingEmail, CancellationToken ct = default)
    {
        var t = await applications.GetTransferAsync(transferId, ct);
        if (t is null || t.Status != TransferStatus.AwaitingIncomingConsent) return Result.Failure(ErrorCodes.NotPending, "This transfer is not awaiting consent.");
        if (!string.Equals(t.IncomingOwnerEmail, incomingEmail?.Trim(), StringComparison.OrdinalIgnoreCase)) return Result.Failure(Guard.Forbidden("Only the named incoming owner can consent."));
        t.IncomingConsentAtUtc = clock.UtcNow; t.Status = TransferStatus.AwaitingReview;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Incoming owner consent recorded.");
    }

    /// <summary>FR-034/035: approval requires both consents. No unilateral transfer on claimant documents alone.</summary>
    public async Task<Result> DecideTransferAsync(Actor admin, int transferId, bool approve, string reason, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return Result.Failure(Guard.Forbidden());
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return Result.Failure(ErrorCodes.Validation, "A decision note is required.");
        var t = await applications.GetTransferAsync(transferId, ct);
        if (t is null) return Result.Failure(Guard.NotFound("Transfer"));
        if (approve && (t.OwnerConsentAtUtc is null || t.IncomingConsentAtUtc is null))
            return Result.Failure(ErrorCodes.ConsentMissing, "Both the current and incoming owner must consent first.");
        t.Status = approve ? TransferStatus.Approved : TransferStatus.Rejected; t.ReviewerAccountId = admin.AccountId; t.DecisionReason = reason.Trim();
        await activity.AuditAsync(admin, approve ? "Approved ownership transfer" : "Rejected ownership transfer", $"transfer:{t.Id}", reason.Trim(), ct: ct);
        await activity.OutboxAsync("ownership.transfer.decided", new { t.Id, approve }, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(approve ? "Transfer approved. The incoming owner receives a provisioning invitation." : "Transfer rejected.");
    }

    public async Task<IReadOnlyList<TransferDto>> ListTransfersAsync(Actor admin, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return [];
        var list = await applications.ListTransfersAsync(ct);
        var names = (await startups.GetManyAsync(list.Select(t => t.StartupId).Distinct().ToList(), ct)).ToDictionary(s => s.Id, s => s.Name);
        return list.Select(t => new TransferDto(t.Id, t.StartupId, names.GetValueOrDefault(t.StartupId, "?"), t.IncomingOwnerName, t.OwnerConsentAtUtc is not null, t.IncomingConsentAtUtc is not null, t.Status)).ToList();
    }
}
