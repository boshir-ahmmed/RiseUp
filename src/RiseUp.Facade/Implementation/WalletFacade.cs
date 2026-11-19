using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Wallets;

public sealed class WalletFacade(IWalletRepository wallets, IUnitOfWork uow, ActivityRecorder activity) : IWalletFacade
{
    public async Task<Result<WalletDto>> GetAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can view the wallet.");
        var wallet = await wallets.GetByStartupAsync(actor.StartupId.Value, ct);
        var ledger = await wallets.ListLedgerAsync(actor.StartupId.Value, 100, ct);
        return Result.Success(new WalletDto(wallet?.WelcomeBalance ?? 0, wallet?.PurchasedBalance ?? 0,
            ledger.Select(l => new LedgerLine(l.Id, l.CreatedAtUtc, l.Description, l.Bucket, l.Amount, l.Reference)).ToList()));
    }

    /// <summary>Finance Administrator compensating entry (FR-131/158). Never edits history.</summary>
    public async Task<Result> AdjustAsync(Actor admin, int startupId, TokenBucket bucket, int amount, string reason, CancellationToken ct = default)
    {
        if (!admin.IsAdmin(AdminRole.FinanceAdmin)) return Result.Failure(Guard.Forbidden());
        if (amount == 0 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            return Result.Failure(ErrorCodes.Validation, "Amount and a reason (at least 5 characters) are required.");
        var wallet = await wallets.GetByStartupAsync(startupId, ct);
        if (wallet is null) return Result.Failure(Guard.NotFound("Wallet"));
        var current = bucket == TokenBucket.Welcome ? wallet.WelcomeBalance : wallet.PurchasedBalance;
        if (current + amount < 0) return Result.Failure(ErrorCodes.NegativeBalance, "Adjustment would make the balance negative.");
        if (bucket == TokenBucket.Welcome) wallet.WelcomeBalance += amount; else wallet.PurchasedBalance += amount;
        await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = startupId, Description = $"Finance adjustment: {reason.Trim()}", Bucket = bucket, Amount = amount, Reference = "finance", IdempotencyKey = $"adj:{Guid.NewGuid():N}", ActorAccountId = admin.AccountId }, ct);
        await activity.AuditAsync(admin, "Ledger adjustment", $"startup:{startupId} {amount:+#;-#} {bucket}", reason.Trim(), ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Compensating entry posted and audited.");
    }
}
