using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Identity;

public sealed class AccountFacade(
    IUserAccountRepository accounts, IStartupRepository startups, IInvestorRepository investors, ITeamAccountRepository team,
    IConnectionRepository connections, IWalletRepository wallets, IVerificationRepository verification,
    IPostRepository posts, IPasswordHasher hasher, IMfaVerifier mfa, IUnitOfWork uow, IClock clock, IValidator<RegisterRequest> validator,
    ActivityRecorder activity, IPromotionFacade promotions) : IAccountFacade
{
    private const string VerifyPurpose = "verify-email";
    private const string ResetPurpose = "reset-password";

    public async Task<Result<RegisteredAccount>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await Guard.ValidateAsync(validator, request, ct) is { } invalid) return invalid;
        var normalized = request.Email.Trim().ToUpperInvariant();
        if (await accounts.FindByNormalizedEmailAsync(normalized, ct) is not null)
            return Result.Failure<RegisteredAccount>(ErrorCodes.EmailTaken, "An account with this email already exists.");

        var now = clock.UtcNow;
        var account = new UserAccount
        {
            Email = request.Email.Trim(), NormalizedEmail = normalized, FullName = request.FullName.Trim(), Phone = request.Phone.Trim(),
            PasswordHash = hasher.Hash(request.Password), PrimaryRole = request.Role,
            AcceptedTermsVersion = BusinessRules.TermsVersion, AcceptedPrivacyVersion = BusinessRules.PrivacyVersion, PolicyAcceptedAtUtc = now,
        };
        await accounts.AddAsync(account, ct);
        await uow.SaveChangesAsync(ct);

        if (request.Role == PrimaryRole.Founder)
        {
            await startups.AddAsync(new Startup
            {
                OwnerAccountId = account.Id, Name = request.StartupName!.Trim(), NormalizedLegalName = request.StartupName.Trim().ToUpperInvariant(),
                Tagline = "", Description = "", Industry = "", Location = "", FoundingYear = now.Year, Stage = FundingStage.PreSeed,
                VerificationStatus = VerificationStatus.Unverified, ColorIndex = account.Id % 10,
            }, ct);
        }
        else
        {
            await investors.AddAsync(new InvestorProfile
            {
                AccountId = account.Id, DisplayName = account.FullName, Affiliation = request.Affiliation?.Trim() ?? "Independent",
                VerificationStatus = VerificationStatus.Unverified, ReceivePitches = true, ColorIndex = account.Id % 10,
            }, ct);
        }

        var token = SecureTokens.NewUrlToken();
        await accounts.AddTokenAsync(new AccountToken { UserAccountId = account.Id, Purpose = VerifyPurpose, TokenHash = SecureTokens.Hash(token), ExpiresAtUtc = now.AddDays(2) }, ct);
        await activity.OutboxAsync("email.verify", new { account.Email, token }, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(new RegisteredAccount(account.Id, token), "Check your email to verify your account.");
    }

    public async Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var record = await accounts.FindTokenAsync(VerifyPurpose, SecureTokens.Hash(token ?? ""), ct);
        if (record is null || record.UsedAtUtc is not null || record.ExpiresAtUtc < clock.UtcNow)
            return Result.Failure(ErrorCodes.TokenInvalid, "This verification link is invalid or has expired.");
        var account = await accounts.GetByIdAsync(record.UserAccountId, ct);
        if (account is null) return Result.Failure(Guard.NotFound("Account"));
        account.EmailVerified = true;
        record.UsedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Email verified. You can now sign in.");
    }

    public async Task<Result<SignedInUser>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var account = await accounts.FindByNormalizedEmailAsync((email ?? "").Trim().ToUpperInvariant(), ct);
        if (account is null || account.DeletedAtUtc is not null)
            return Result.Failure<SignedInUser>(ErrorCodes.InvalidCredentials, "Email or password is incorrect.");
        if (account.LockoutEndUtc > now)
            return Result.Failure<SignedInUser>(ErrorCodes.AccountLocked, "Too many failed attempts. Try again in 15 minutes.");

        if (!hasher.Verify(account.PasswordHash, password ?? ""))
        {
            account.FailedLoginCount++;
            if (account.FailedLoginCount >= BusinessRules.MaxFailedLogins)
            {
                account.LockoutEndUtc = now.Add(BusinessRules.LockoutDuration);
                account.FailedLoginCount = 0;
            }
            await uow.SaveChangesAsync(ct);
            return Result.Failure<SignedInUser>(ErrorCodes.InvalidCredentials, "Email or password is incorrect.");
        }

        if (!account.EmailVerified) return Result.Failure<SignedInUser>(ErrorCodes.EmailNotVerified, "Please verify your email before signing in.");
        account.FailedLoginCount = 0;
        account.LockoutEndUtc = null;
        await uow.SaveChangesAsync(ct);

        int? startupId = null, investorId = null;
        if (account.PrimaryRole == PrimaryRole.Founder) startupId = (await startups.GetByOwnerAsync(account.Id, ct))?.Id;
        if (account.PrimaryRole == PrimaryRole.Investor) investorId = (await investors.GetByAccountAsync(account.Id, ct))?.Id;
        var mfaRequired = account.PrimaryRole == PrimaryRole.Admin || account.MfaEnabled;
        return Result.Success(new SignedInUser(account.Id, null, account.FullName, account.PrimaryRole, account.AdminRole, startupId, investorId, mfaRequired));
    }

    public async Task<Result<SignedInUser>> SignInTeamMemberAsync(string username, string password, CancellationToken ct = default)
    {
        var member = await team.FindByUsernameAsync((username ?? "").Trim(), ct);
        if (member is null || member.Status != TeamAccountStatus.Active || !hasher.Verify(member.PasswordHash, password ?? ""))
            return Result.Failure<SignedInUser>(ErrorCodes.InvalidCredentials, "Username or password is incorrect.");
        return Result.Success(new SignedInUser(null, member.Id, member.FullName, PrimaryRole.TeamMember, AdminRole.None, member.StartupId, null, false));
    }

    public async Task<Result> VerifyMfaAsync(int accountId, string code, CancellationToken ct = default)
    {
        var account = await accounts.GetByIdAsync(accountId, ct);
        if (account is null) return Result.Failure(Guard.NotFound("Account"));
        return mfa.Verify(account.MfaSecret, code) ? Result.Success() : Result.Failure(ErrorCodes.InvalidCredentials, "The authentication code is not valid.");
    }

    public async Task<Result<string>> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var account = await accounts.FindByNormalizedEmailAsync((email ?? "").Trim().ToUpperInvariant(), ct);
        // Same response whether or not the account exists (no account enumeration).
        if (account is null) return Result.Success(string.Empty, "If an account exists, a reset link has been sent.");
        var token = SecureTokens.NewUrlToken();
        await accounts.AddTokenAsync(new AccountToken { UserAccountId = account.Id, Purpose = ResetPurpose, TokenHash = SecureTokens.Hash(token), ExpiresAtUtc = clock.UtcNow.Add(BusinessRules.PasswordResetLifetime) }, ct);
        await activity.OutboxAsync("email.password-reset", new { account.Email, token }, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(token, "If an account exists, a reset link has been sent.");
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 10 || !newPassword.Any(char.IsDigit) || !newPassword.Any(char.IsLetter))
            return Result.Failure(ErrorCodes.Validation, "Password must be at least 10 characters and include letters and numbers.");
        var record = await accounts.FindTokenAsync(ResetPurpose, SecureTokens.Hash(token ?? ""), ct);
        if (record is null || record.UsedAtUtc is not null || record.ExpiresAtUtc < clock.UtcNow)
            return Result.Failure(ErrorCodes.TokenInvalid, "This reset link is invalid, already used or older than 60 minutes.");
        var account = await accounts.GetByIdAsync(record.UserAccountId, ct);
        if (account is null) return Result.Failure(Guard.NotFound("Account"));
        account.PasswordHash = hasher.Hash(newPassword);
        record.UsedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Password updated. Please sign in.");
    }

    /// <summary>FR-161–166: revoke access, anonymise, cancel requests, forfeit credits, erase identity evidence.</summary>
    public async Task<Result> DeleteAccountAsync(Actor actor, string confirmation, CancellationToken ct = default)
    {
        if (confirmation != "DELETE") return Result.Failure(ErrorCodes.Validation, "Type DELETE to confirm.");
        if (actor.AccountId is null || !(actor.IsFounder || actor.IsInvestor)) return Result.Failure(Guard.Forbidden());
        var account = await accounts.GetByIdAsync(actor.AccountId.Value, ct);
        if (account is null) return Result.Failure(Guard.NotFound("Account"));
        var now = clock.UtcNow;
        var cancelled = 0;

        if (actor.IsFounder && actor.StartupId is int sid && await startups.GetByIdAsync(sid, ct) is { } startup)
        {
            foreach (var r in await connections.ListPendingForStartupAsync(sid, ct)) { r.Status = Entities.Common.RequestStatus.Cancelled; r.DecidedAtUtc = now; r.DecisionNote = "account deleted"; cancelled++; }
            if (await wallets.GetByStartupAsync(sid, ct) is { } wallet)
            {
                if (wallet.WelcomeBalance > 0) await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = sid, Description = "Forfeited on account deletion", Bucket = TokenBucket.Welcome, Amount = -wallet.WelcomeBalance, Reference = "deletion", IdempotencyKey = $"forfeit:w:{sid}" }, ct);
                if (wallet.PurchasedBalance > 0) await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = sid, Description = "Forfeited on account deletion", Bucket = TokenBucket.Purchased, Amount = -wallet.PurchasedBalance, Reference = "deletion", IdempotencyKey = $"forfeit:p:{sid}" }, ct);
                wallet.WelcomeBalance = 0; wallet.PurchasedBalance = 0;
            }
            foreach (var p in await posts.ListByStartupAsync(sid, false, ct)) { var tracked = await posts.GetByIdAsync(p.Id, ct); if (tracked is not null) tracked.IsHidden = true; }
            foreach (var t in await team.ListForStartupAsync(sid, ct)) if (t.Status == TeamAccountStatus.Active) t.Status = TeamAccountStatus.Deactivated;
            await promotions.EndAllForStartupAsync(sid, ct);
            startup.IsDeleted = true; startup.Name = "Deleted startup"; startup.Description = ""; startup.Tagline = "";
        }
        if (actor.IsInvestor && await investors.GetByIdAsync(actor.InvestorId!.Value, ct) is { } investor)
        {
            foreach (var r in await connections.ListPendingForInvestorAsync(investor.Id, ct)) { r.Status = Entities.Common.RequestStatus.Cancelled; r.DecidedAtUtc = now; r.DecisionNote = "account deleted"; cancelled++; }
            investor.IsDeleted = true; investor.DisplayName = "Deleted investor"; investor.Bio = "";
        }
        foreach (var app in await verification.ListAsync(null, ct))
        {
            if (app.StartupId == actor.StartupId || app.InvestorId == actor.InvestorId)
            {
                var tracked = await verification.GetByIdAsync(app.Id, ct);
                tracked?.Documents.ForEach(d => { d.Erased = true; d.StorageKey = "erased"; });
            }
        }
        account.DeletedAtUtc = now; account.FullName = "Deleted user"; account.Phone = null; account.PasswordHash = "";
        account.Email = $"deleted-{account.Id}@invalid"; account.NormalizedEmail = account.Email.ToUpperInvariant();
        await activity.AuditAsync(Actor.System, "Account permanently deleted", $"account:{account.Id}", $"{cancelled} pending request(s) cancelled; balances forfeited; identity documents erased", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success($"Your account was deleted. {cancelled} pending request(s) were cancelled.");
    }
}
