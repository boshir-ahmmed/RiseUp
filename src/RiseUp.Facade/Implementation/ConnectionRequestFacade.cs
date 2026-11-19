using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.ConnectionRequests;

public sealed class ConnectionRequestFacade(IConnectionRepository requests, IStartupRepository startups, IInvestorRepository investors,
    IWalletRepository wallets, IConversationRepository conversations, IContentSafetyModel model1, IUnitOfWork uow, IClock clock,
    ActivityRecorder activity, ILogger<ConnectionRequestFacade> logger, IPitchRepository pitchPackages, IMediaRepository media) : IConnectionRequestFacade
{
    private static readonly TimeSpan Model1Timeout = TimeSpan.FromSeconds(8);

    // ---------------------------------------------------------------- shared eligibility
    private async Task<Error?> PairBlockersAsync(int startupId, int investorId, RequestType type, CancellationToken ct)
    {
        if (await requests.GetBlockAsync(startupId, investorId, ct) is not null) return new Error(ErrorCodes.Blocked, "A block exists between this startup and investor.");
        if (await requests.GetConnectionAsync(startupId, investorId, ct) is not null) return new Error(ErrorCodes.AlreadyConnected, "You are already connected; use your existing conversation.");
        if (await requests.GetPendingForPairAsync(startupId, investorId, ct) is not null)
            return new Error(ErrorCodes.PendingRequestExists, "A pending pitch or invitation already exists for this pair. Nothing was charged.");
        // FR-075/086: after an early rejection, resend only after ORIGINAL send + 14 days (ExpiresAtUtc).
        if (await requests.GetActiveCooldownAsync(startupId, investorId, type, clock.UtcNow, ct) is { } rejected)
            return new Error(ErrorCodes.CooldownActive, $"The previous {type.ToString().ToLowerInvariant()} was declined. You can send again after {rejected.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC.");
        return null;
    }

    private async Task<Error?> PitchEligibilityAsync(Startup startup, InvestorProfile? investor, CancellationToken ct)
    {
        if (startup.IsSuspended) return new Error(ErrorCodes.AccountSuspended, "Your startup account is suspended.");
        if (!startup.IsActiveVerified) return new Error(ErrorCodes.NotVerified, "Your startup must be verified before pitching.");
        if (investor is null || !investor.IsActiveVerified) return new Error(ErrorCodes.InvestorNotVerified, "This investor is not verified.");
        if (await PairBlockersAsync(startup.Id, investor.Id, RequestType.Pitch, ct) is { } e) return e;
        if (!investor.ReceivePitches) return new Error(ErrorCodes.PitchDisabled, "This investor is not accepting pitches right now. No credit was charged.");
        var wallet = await wallets.GetByStartupAsync(startup.Id, ct);
        if (wallet is null || wallet.WelcomeBalance + wallet.PurchasedBalance < 1) return new Error(ErrorCodes.InsufficientTokens, "You have no credits left. Buy credits to send a pitch.");
        return null;
    }

    // ---------------------------------------------------------------- founder pitch
    public async Task<Result<int>> SaveDraftAsync(Actor actor, int? investorId, string text, int? draftId, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can write pitches.");
        if (string.IsNullOrWhiteSpace(text) || text.Length > BusinessRules.PitchMax) return new Error(ErrorCodes.Validation, $"Pitch text is required (max {BusinessRules.PitchMax:N0} characters).");
        var draft = draftId is int id ? await requests.GetDraftAsync(id, ct) : null;
        if (draft is not null && draft.StartupId != actor.StartupId) return Guard.Forbidden();
        if (draft is null) { draft = new PitchDraft { StartupId = actor.StartupId.Value }; await requests.AddDraftAsync(draft, ct); }
        draft.InvestorId = investorId; draft.Text = text; draft.State = ModerationState.None; draft.ReasonCode = null;
        await uow.SaveChangesAsync(ct);
        return Result.Success(draft.Id, "Draft saved.");
    }

    public async Task<IReadOnlyList<DraftDto>> ListDraftsAsync(Actor actor, CancellationToken ct = default) =>
        !actor.IsFounder || actor.StartupId is null ? [] :
        (await requests.ListDraftsAsync(actor.StartupId.Value, ct)).Select(d => new DraftDto(d.Id, d.InvestorId, d.Text, d.State, d.ReasonCode)).ToList();

    /// <summary>
    /// Founder pitch (master prompt §9): manual text → preflight → AI Model 1 (fail-closed) → recheck inside a
    /// serializable transaction → debit 1 credit (Welcome first) + append ledger + create Pending request → commit.
    /// Rejection, AI outage or any ineligibility leaves an editable draft and costs nothing.
    /// </summary>
    public async Task<Result<int>> SendPitchAsync(Actor actor, SendPitchRequest request, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can pitch.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) return new Error(ErrorCodes.Validation, "Missing idempotency key.");
        // Idempotent replay (double click / retried POST): return the original request, never debit twice.
        var replayKey = $"pitch:{actor.StartupId}:{request.IdempotencyKey}";
        if (await wallets.LedgerKeyExistsAsync(replayKey, ct)) return new Error(ErrorCodes.PendingRequestExists, "This pitch was already delivered.");

        var draftResult = await SaveDraftAsync(actor, request.InvestorId, request.Text, request.DraftId, ct);
        if (draftResult.IsFailure) return draftResult;
        var draft = (await requests.GetDraftAsync(draftResult.Value, ct))!;

        var startup = (await startups.GetByIdAsync(actor.StartupId.Value, ct))!;
        var investor = await investors.GetByIdAsync(request.InvestorId, ct);
        if (await PitchEligibilityAsync(startup, investor, ct) is { } pre) return pre;

        draft.State = ModerationState.PendingCheck;
        ContentSafetyResult safety;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Model1Timeout);
            safety = await model1.CheckAsync("pitch", request.Text, cts.Token);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("AI Model 1 failed for pitch draft {DraftId}: {Error}", draft.Id, ex.GetType().Name);
            safety = new ContentSafetyResult(SafetyOutcome.Unavailable, null, "unknown");
        }
        if (safety.Outcome == SafetyOutcome.Unavailable)
        {
            draft.State = ModerationState.AiUnavailable;
            await uow.SaveChangesAsync(ct);
            return new Error(ErrorCodes.AiTemporarilyUnavailable, "The safety check is unavailable. Your draft is saved, nothing was sent and no credit was charged.");
        }
        if (safety.Outcome == SafetyOutcome.Reject)
        {
            draft.State = ModerationState.Rejected; draft.ReasonCode = safety.ReasonCode;
            await uow.SaveChangesAsync(ct);
            return new Error(ErrorCodes.ContentRejected, $"The safety check rejected this pitch ({safety.ReasonCode}). Edit and resubmit — no credit was charged.");
        }

        await using var tx = await uow.BeginTransactionAsync(ct);
        // Recheck at commit time: state may have changed while the model was running (master prompt §9).
        investor = await investors.GetByIdAsync(request.InvestorId, ct);
        if (await PitchEligibilityAsync(startup, investor, ct) is { } recheck) { draft.State = ModerationState.None; await uow.SaveChangesAsync(ct); await tx.CommitAsync(ct); return recheck; }

        var wallet = (await wallets.GetByStartupAsync(startup.Id, ct))!;
        var bucket = wallet.WelcomeBalance > 0 ? TokenBucket.Welcome : TokenBucket.Purchased;   // FR-125
        if (bucket == TokenBucket.Welcome) wallet.WelcomeBalance--; else wallet.PurchasedBalance--;
        var now = clock.UtcNow;
        var pitch = new ConnectionRequest
        {
            Type = RequestType.Pitch, StartupId = startup.Id, InvestorId = investor!.Id, Message = request.Text.Trim(), SentAtUtc = now,
            ExpiresAtUtc = now.Add(BusinessRules.RequestWindow), Status = RequestStatus.Pending, TokenBucket = bucket,
            AiDecisionVersion = safety.ModelVersion, IdempotencyKey = replayKey,
        };
        // v3: snapshot the package version and the documents the founder chose (only clean documents from their own package).
        if (await pitchPackages.GetPackageAsync(startup.Id, ct) is { } package)
        {
            var clean = (await media.GetManyAsync(package.Documents.Select(d => d.MediaId).ToList(), ct)).Where(a => a.ScanStatus == ScanStatus.Clean).Select(a => a.Id).ToHashSet();
            var allowed = package.Documents.Where(d => clean.Contains(d.MediaId)).Select(d => d.Id).ToHashSet();
            var chosen = request.DocumentIds is null ? allowed : request.DocumentIds.Where(allowed.Contains).ToHashSet();
            pitch.PackageVersion = package.PackageVersion;
            pitch.AttachedDocumentIds = string.Join(',', chosen.OrderBy(x => x));
        }
        await requests.AddAsync(pitch, ct);
        await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = startup.Id, Description = $"Pitch delivered → {investor.DisplayName}", Bucket = bucket, Amount = -1, Reference = $"pitch:{request.IdempotencyKey}", IdempotencyKey = replayKey, ActorAccountId = actor.AccountId }, ct);
        requests.RemoveDraft(draft);
        await activity.NotifyAccountAsync(investor.AccountId, $"New pitch from {startup.Name}.", "/requests", ct);
        try
        {
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException)
        {
            // Unique pending-pair index or wallet concurrency token rejected a racing request: nothing is debited.
            await tx.RollbackAsync(ct);
            return new Error(ErrorCodes.PendingRequestExists, "Another request for this pair was submitted at the same time. No credit was charged.");
        }
        return Result.Success(pitch.Id, $"Pitch delivered to {investor.DisplayName}. 1 {bucket.ToString().ToLowerInvariant()} credit used.");
    }

    // ---------------------------------------------------------------- investor invitation
    public async Task<Result<int>> SendInvitationAsync(Actor actor, int startupId, string? note, string idempotencyKey, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return Guard.Forbidden("Only verified investors can send invitations.");
        var investor = await investors.GetByIdAsync(actor.InvestorId!.Value, ct);
        if (investor is null || !investor.IsActiveVerified) return new Error(ErrorCodes.NotVerified, "Only verified investors can send invitations.");
        var startup = await startups.GetByIdAsync(startupId, ct);
        if (startup is null || !startup.IsActiveVerified) return new Error(ErrorCodes.StartupNotVerified, "Invitations can only be sent to verified startups.");
        if (note?.Length > 500) return new Error(ErrorCodes.Validation, "Notes are limited to 500 characters.");
        // FR-081: Receive Pitches has no effect on invitations. FR-087: blocked/duplicate attempts never consume allowance.
        if (await PairBlockersAsync(startup.Id, investor.Id, RequestType.Invitation, ct) is { } blocked) return blocked;

        await using var tx = await uow.BeginTransactionAsync(ct);
        var now = clock.UtcNow;
        var today = DhakaTime.ToDhakaDate(now);   // FR-082/083: Asia/Dhaka civil day, resets at 00:00, no carry-over
        var counter = await requests.GetCounterAsync(investor.Id, today, ct);
        if (counter is null) { counter = new InvitationDailyCounter { InvestorId = investor.Id, DhakaDate = today }; await requests.AddCounterAsync(counter, ct); }
        if (counter.Count >= BusinessRules.DailyInvitationLimit)
            return new Error(ErrorCodes.InvitationDailyLimit, "You have used all 10 invitations for today (Asia/Dhaka). Your allowance resets at midnight.");
        counter.Count++;
        var invitation = new ConnectionRequest
        {
            Type = RequestType.Invitation, StartupId = startup.Id, InvestorId = investor.Id, Message = note?.Trim() ?? "", SentAtUtc = now,
            ExpiresAtUtc = now.Add(BusinessRules.RequestWindow), Status = RequestStatus.Pending, IdempotencyKey = $"invite:{investor.Id}:{idempotencyKey}",
        };
        await requests.AddAsync(invitation, ct);
        await activity.NotifyAccountAsync(startup.OwnerAccountId, $"{investor.DisplayName} sent you an invitation.", "/requests", ct);
        try
        {
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException)
        {
            await tx.RollbackAsync(ct);
            return new Error(ErrorCodes.PendingRequestExists, "A request for this pair was created at the same time. Your daily allowance was not used.");
        }
        return Result.Success(invitation.Id, $"Invitation sent to {startup.Name}. {BusinessRules.DailyInvitationLimit - counter.Count} left today.");
    }

    public async Task<int> InvitationsUsedTodayAsync(int investorId, CancellationToken ct = default) =>
        (await requests.GetCounterAsync(investorId, DhakaTime.ToDhakaDate(clock.UtcNow), ct))?.Count ?? 0;

    // ---------------------------------------------------------------- decisions
    /// <summary>
    /// Accepting creates exactly ONE connection and ONE group chat (owner + investor), protected by unique indexes
    /// and a serializable transaction against concurrent double-accepts (FR-089/090). Declines never refund credits.
    /// </summary>
    public async Task<Result> RespondAsync(Actor actor, int requestId, bool accept, string? note, CancellationToken ct = default)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        var r = await requests.GetByIdAsync(requestId, ct);
        if (r is null) return Result.Failure(Guard.NotFound("Request"));
        var isRecipient = r.Type == RequestType.Pitch ? actor.IsInvestor && actor.InvestorId == r.InvestorId
                                                      : actor.IsFounder && actor.StartupId == r.StartupId;   // FR-084: owner, not team
        if (!isRecipient) return Result.Failure(Guard.Forbidden("Only the recipient can decide this request."));
        var now = clock.UtcNow;
        if (r.Status == RequestStatus.Pending && now >= r.ExpiresAtUtc) { r.Status = RequestStatus.Expired; r.DecidedAtUtc = r.ExpiresAtUtc; await uow.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        if (r.Status != RequestStatus.Pending) return Result.Failure(ErrorCodes.NotPending, "This request is no longer pending.");

        var startup = (await startups.GetByIdAsync(r.StartupId, ct))!;
        var investor = (await investors.GetByIdAsync(r.InvestorId, ct))!;
        r.DecidedAtUtc = now; r.DecisionNote = note?.Trim();
        if (!accept)
        {
            r.Status = RequestStatus.Rejected;
            await activity.NotifyAccountAsync(r.Type == RequestType.Pitch ? startup.OwnerAccountId : investor.AccountId,
                $"{(r.Type == RequestType.Pitch ? investor.DisplayName : startup.Name)} declined your {r.Type.ToString().ToLowerInvariant()}.", "/requests", ct);
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result.Success("Declined. A new request is possible 14 days after the original send time.");
        }
        if (await requests.GetConnectionAsync(r.StartupId, r.InvestorId, ct) is not null) return Result.Failure(ErrorCodes.AlreadyConnected, "Already connected.");

        r.Status = RequestStatus.Accepted;
        var conversation = new Conversation { StartupId = r.StartupId, InvestorId = r.InvestorId };
        conversation.Members.Add(new ConversationMember { Kind = MemberKind.Owner, AccountId = startup.OwnerAccountId, AddedAtUtc = now });
        conversation.Members.Add(new ConversationMember { Kind = MemberKind.Investor, AccountId = investor.AccountId, AddedAtUtc = now });
        await conversations.AddAsync(conversation, ct);
        try
        {
            await uow.SaveChangesAsync(ct);   // assigns conversation id
            await conversations.AddMessageAsync(new Message { ConversationId = conversation.Id, AuthorKind = MemberKind.Owner, AuthorName = "RiseUp", IsSystem = true, SentAtUtc = now,
                Body = "Connection established. Contact details are now visible to both parties. Investment agreements and transfers happen outside RiseUp." }, ct);
            await requests.AddConnectionAsync(new StartupInvestorConnection { StartupId = r.StartupId, InvestorId = r.InvestorId, ConversationId = conversation.Id }, ct);
            await activity.NotifyAccountAsync(startup.OwnerAccountId, $"You are now connected with {investor.DisplayName}.", "/conversations", ct);
            await activity.NotifyAccountAsync(investor.AccountId, $"You are now connected with {startup.Name}.", "/conversations", ct);
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException)
        {
            await tx.RollbackAsync(ct);
            return Result.Failure(ErrorCodes.AlreadyConnected, "This pair was connected by a concurrent request.");
        }
        return Result.Success("Accepted — one connection and one private group chat were created.");
    }

    public async Task<IReadOnlyList<RequestDto>> ListForActorAsync(Actor actor, CancellationToken ct = default)
    {
        IReadOnlyList<ConnectionRequest> list = actor.IsFounder && actor.StartupId is int sid ? await requests.ListForStartupAsync(sid, ct)
            : actor.IsInvestor ? await requests.ListForInvestorAsync(actor.InvestorId!.Value, ct) : [];
        var sNames = (await startups.GetManyAsync(list.Select(r => r.StartupId).Distinct().ToList(), ct)).ToDictionary(s => s.Id, s => s.Name);
        var iNames = (await investors.GetManyAsync(list.Select(r => r.InvestorId).Distinct().ToList(), ct)).ToDictionary(i => i.Id, i => i.DisplayName);
        return list.Select(r => new RequestDto(r.Id, r.Type, r.StartupId, sNames.GetValueOrDefault(r.StartupId, "?"), r.InvestorId, iNames.GetValueOrDefault(r.InvestorId, "?"),
            r.Message, r.SentAtUtc, r.ExpiresAtUtc, r.Status, r.DecisionNote, r.TokenBucket, r.Status == RequestStatus.Rejected ? r.ExpiresAtUtc : null)).ToList();
    }

    public async Task<int> ExpireDueAsync(CancellationToken ct = default)
    {
        var due = await requests.ListPendingDueAsync(clock.UtcNow, 500, ct);
        foreach (var r in due) { r.Status = RequestStatus.Expired; r.DecidedAtUtc = r.ExpiresAtUtc; }   // no refunds (FR-077)
        if (due.Count > 0) await uow.SaveChangesAsync(ct);
        return due.Count;
    }
}
