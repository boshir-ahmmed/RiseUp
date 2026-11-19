using System.Security.Cryptography;
using System.Text;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Payments;

public sealed class PaymentFacade(IPaymentRepository payments, IWalletRepository wallets, IStartupRepository startups, IPaymentGateway gateway,
    IUnitOfWork uow, IClock clock, ActivityRecorder activity) : IPaymentFacade
{
    public async Task<IReadOnlyList<PackageDto>> ListPackagesAsync(CancellationToken ct = default) =>
        (await payments.ListActivePackagesAsync(ct)).Select(p => new PackageDto(p.Id, p.Tokens, p.PriceBdt)).ToList();

    public async Task<Result<CheckoutDto>> CreateOrderAsync(Actor actor, int packageId, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can buy tokens.");
        var startup = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (startup is null || !startup.IsActiveVerified) return new Error(ErrorCodes.NotVerified, "Token purchases open after verification.");
        var package = await payments.GetPackageAsync(packageId, ct);
        if (package is null) return Guard.NotFound("Token package");
        var order = new PaymentOrder
        {
            OrderNumber = $"PO-{clock.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(100000, 999999)}", StartupId = startup.Id, PackageId = package.Id,
            TokensSnapshot = package.Tokens, PriceBdtSnapshot = package.PriceBdt, Status = PaymentStatus.Pending,
        };
        await payments.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        var session = await gateway.CreateCheckoutAsync(order.OrderNumber, order.PriceBdtSnapshot, ct);
        return Result.Success(new CheckoutDto(order.OrderNumber, session.RedirectUrl, session.IsSandbox));
    }

    public async Task<Result> HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? "")));
        var parts = (payload ?? "").Split('|');
        if (!gateway.VerifySignature(payload ?? "", signature) || parts.Length != 3)
        {
            await payments.AddEventAsync(new PaymentProviderEvent { ProviderTransactionId = parts.ElementAtOrDefault(1) ?? "?", SignatureValid = false, PayloadHash = payloadHash, Result = "Rejected — invalid signature" }, ct);
            await uow.SaveChangesAsync(ct);
            return Result.Failure(ErrorCodes.InvalidSignature, "Webhook signature invalid. Nothing credited.");
        }
        var (orderNumber, txn, status) = (parts[0], parts[1], parts[2]);
        var order = await payments.GetByOrderNumberAsync(orderNumber, ct);
        if (order is null) return Result.Failure(Guard.NotFound("Order"));
        if (status != "PAID")
        {
            if (order.CreditedAtUtc is null) order.Status = PaymentStatus.Failed;
            await payments.AddEventAsync(new PaymentProviderEvent { OrderId = order.Id, ProviderTransactionId = txn, SignatureValid = true, PayloadHash = payloadHash, Result = $"Gateway reported {status}" }, ct);
            await uow.SaveChangesAsync(ct);
            return Result.Failure(ErrorCodes.Validation, "Payment was not successful. Nothing credited.");
        }
        return await CreditAsync(order, txn, payloadHash, "Gateway confirmation", ct);
    }

    public async Task<Result> ReconcileAsync(Actor admin, string orderNumber, string providerTransactionId, string reason, CancellationToken ct = default)
    {
        if (!admin.IsAdmin(AdminRole.FinanceAdmin)) return Result.Failure(Guard.Forbidden());
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return Result.Failure(ErrorCodes.Validation, "A settlement reason is required.");
        var order = await payments.GetByOrderNumberAsync(orderNumber, ct);
        if (order is null) return Result.Failure(Guard.NotFound("Order"));
        await activity.AuditAsync(admin, "Manual payment reconciliation", order.OrderNumber, reason.Trim(), ct: ct);
        return await CreditAsync(order, providerTransactionId, "manual", "Manual reconciliation", ct);
    }

    private async Task<Result> CreditAsync(PaymentOrder order, string txn, string payloadHash, string method, CancellationToken ct)
    {
        await using var tx = await uow.BeginTransactionAsync(ct);
        if (order.CreditedAtUtc is not null || await payments.ProviderTransactionUsedAsync(txn, ct) || await wallets.LedgerKeyExistsAsync($"order:{txn}", ct))
        {
            await payments.AddEventAsync(new PaymentProviderEvent { OrderId = order.Id, ProviderTransactionId = txn, SignatureValid = true, PayloadHash = payloadHash, Result = "Duplicate — ignored (already credited)" }, ct);
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result.Failure(ErrorCodes.DuplicatePaymentEvent, "Duplicate confirmation ignored. Tokens were credited only once.");
        }
        var wallet = await wallets.GetByStartupAsync(order.StartupId, ct);
        if (wallet is null) { wallet = new StartupWallet { StartupId = order.StartupId }; await wallets.AddAsync(wallet, ct); }
        wallet.PurchasedBalance += order.TokensSnapshot;
        order.Status = PaymentStatus.Paid; order.ProviderTransactionId = txn; order.CreditedAtUtc = clock.UtcNow; order.Method = method;
        await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = order.StartupId, Description = $"Token purchase · {order.TokensSnapshot} tokens (order {order.OrderNumber})", Bucket = TokenBucket.Purchased, Amount = order.TokensSnapshot, Reference = $"order:{order.OrderNumber}", IdempotencyKey = $"order:{txn}" }, ct);
        await payments.AddEventAsync(new PaymentProviderEvent { OrderId = order.Id, ProviderTransactionId = txn, SignatureValid = true, PayloadHash = payloadHash, Result = $"Credited {order.TokensSnapshot} Purchased" }, ct);
        var owner = (await startups.GetByIdAsync(order.StartupId, ct))?.OwnerAccountId;
        await activity.NotifyAccountAsync(owner, $"Payment received: {order.TokensSnapshot} tokens added.", "/wallet", ct);
        try
        {
            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PersistenceConflictException)
        {
            await tx.RollbackAsync(ct);
            return Result.Failure(ErrorCodes.DuplicatePaymentEvent, "Duplicate confirmation ignored. Tokens were credited only once.");
        }
        return Result.Success($"Verified payment — {order.TokensSnapshot} Purchased tokens credited.");
    }

    public async Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Actor actor, CancellationToken ct = default)
    {
        int? scope = actor.IsAdmin(AdminRole.FinanceAdmin) ? null : actor.IsFounder ? actor.StartupId : -1;
        if (scope == -1) return [];
        return (await payments.ListOrdersAsync(scope, 100, ct))
            .Select(o => new OrderDto(o.OrderNumber, o.StartupId, o.TokensSnapshot, o.PriceBdtSnapshot, o.Status, o.ProviderTransactionId, o.CreditedAtUtc is not null, o.CreatedAtUtc)).ToList();
    }
}
