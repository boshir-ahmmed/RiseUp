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
    IUnitOfWork uow, IClock clock, ActivityRecorder activity, IUserAccountRepository accounts) : IPaymentFacade
{
    public PaymentMode Mode => gateway.Mode;
    private string OrderPrefix => Mode switch { PaymentMode.Live => "RL-", PaymentMode.HostedTest => "RT-", _ => "PO-" };

    public async Task<IReadOnlyList<PackageDto>> ListPackagesAsync(CancellationToken ct = default) =>
        (await payments.ListActivePackagesAsync(ct)).Select(p => new PackageDto(p.Id, p.Tokens, p.PriceBdt)).ToList();

    public async Task<Result<CheckoutDto>> CreateOrderAsync(Actor actor, int packageId, BillingAddress? billing = null, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return Guard.Forbidden("Only the Main Startup Owner can buy credits.");
        if (Mode == PaymentMode.Disabled) return new Error(ErrorCodes.PaymentUnavailable, "Credit purchases are currently closed.");
        var startup = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (startup is null || !startup.IsActiveVerifiedAt(clock.UtcNow)) return new Error(ErrorCodes.NotVerified, "Credit purchases open after verification.");
        var package = await payments.GetPackageAsync(packageId, ct);
        if (package is null) return Guard.NotFound("Credit package");
        var buyer = await accounts.GetByIdAsync(startup.OwnerAccountId, ct);
        if (buyer is null || buyer.Id != actor.AccountId || buyer.DeletedAtUtc != null) return Guard.Forbidden();
        var phone = string.IsNullOrWhiteSpace(billing?.Phone) ? buyer.Phone : billing.Phone.Trim();
        if (gateway is IHostedPaymentGateway && (billing is null ||
            new[] { billing.Address, billing.City, billing.Country }.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 50) ||
            string.IsNullOrWhiteSpace(billing.Postcode) || billing.Postcode.Length > 30 ||
            string.IsNullOrWhiteSpace(phone) || phone.Length > 20 || buyer.Email.Length > 50))
            return new Error(ErrorCodes.Validation, "Enter your billing address, city, postal code and country. Checkout requires an account phone and email supported by the provider.");
        var order = new PaymentOrder
        {
            OrderNumber = OrderPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)), StartupId = startup.Id, PackageId = package.Id,
            TokensSnapshot = package.Tokens, PriceBdtSnapshot = package.PriceBdt, Status = PaymentStatus.Pending,
        };
        await payments.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        try
        {
            var customer = new CheckoutCustomer(buyer.FullName[..Math.Min(50, buyer.FullName.Length)], buyer.Email, phone ?? "", billing);
            var session = await gateway.CreateCheckoutAsync(order.OrderNumber, order.PriceBdtSnapshot, customer, ct);
            return Result.Success(new CheckoutDto(order.OrderNumber, session.RedirectUrl, session.IsSandbox));
        }
        catch (Exception ex) when (ProviderUnavailable(ex, ct))
        {
            return new Error(ErrorCodes.PaymentUnavailable, $"Checkout could not be started. Order {order.OrderNumber} is pending verification.");
        }
    }

    public async Task<Result> HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        if (Mode != PaymentMode.Simulator) return Result.Failure(ErrorCodes.InvalidSignature, "Simulator callbacks are disabled.");
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
        if (order is null || !order.OrderNumber.StartsWith(OrderPrefix, StringComparison.Ordinal)) return Result.Failure(Guard.NotFound("Order"));
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
        if (order is null || !order.OrderNumber.StartsWith(OrderPrefix, StringComparison.Ordinal)) return Result.Failure(Guard.NotFound("Order"));
        if (Mode == PaymentMode.Disabled) return Result.Failure(ErrorCodes.PaymentUnavailable, "Configure the original gateway to reconcile this order.");
        if (gateway is IHostedPaymentGateway)
        {
            await activity.AuditAsync(admin, "Requested provider reconciliation", order.OrderNumber, reason.Trim(), ct: ct);
            await uow.SaveChangesAsync(ct);
            return await ConfirmHostedAsync(orderNumber, null, ct);
        }
        if (string.IsNullOrWhiteSpace(providerTransactionId) || providerTransactionId.Length > 100)
            return Result.Failure(ErrorCodes.Validation, "Enter the simulator transaction reference.");
        await activity.AuditAsync(admin, "Manual payment reconciliation", order.OrderNumber, reason.Trim(), ct: ct);
        return await CreditAsync(order, providerTransactionId, "manual", "Manual reconciliation", ct);
    }

    private static bool ProviderUnavailable(Exception ex, CancellationToken ct) => !ct.IsCancellationRequested &&
        ex is HttpRequestException or IOException or InvalidDataException or System.Text.Json.JsonException or TaskCanceledException;

    public Task<IReadOnlyList<string>> DueForReconciliationAsync(CancellationToken ct = default) =>
        gateway is IHostedPaymentGateway ? payments.ListDueOrdersAsync(OrderPrefix, clock.UtcNow, ct) : Task.FromResult<IReadOnlyList<string>>([]);

    public async Task<Result> ConfirmHostedAsync(string orderNumber, string? validationId, CancellationToken ct = default)
    {
        if (gateway is not IHostedPaymentGateway hosted || string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 30 ||
            !orderNumber.StartsWith(OrderPrefix, StringComparison.Ordinal) || validationId?.Length > 100)
            return Result.Failure(Guard.NotFound("Payment order"));
        var order = await payments.GetByOrderNumberAsync(orderNumber, ct);
        if (order is null) return Result.Failure(Guard.NotFound("Payment order"));
        ProviderPayment result;
        try { result = await hosted.CheckAsync(orderNumber, validationId, ct); }
        catch (Exception ex) when (ProviderUnavailable(ex, ct))
        {
            return Result.Failure(ErrorCodes.PaymentUnavailable, "The gateway could not confirm this payment. Retry verification.");
        }
        var paid = result.Outcome == ProviderOutcome.Paid && result.OrderNumber == order.OrderNumber &&
            result.Amount == order.PriceBdtSnapshot && result.Currency == order.Currency && !string.IsNullOrWhiteSpace(result.BankReference);
        if (paid && order.CreditedAtUtc == null && !await payments.ProviderTransactionUsedAsync(result.BankReference, ct) &&
            await startups.GetByIdAsync(order.StartupId, ct) is { IsDeleted: false })
            return await CreditAsync(order, result.BankReference, "provider-validation", "SSLCOMMERZ " + result.Method[..Math.Min(result.Method.Length, 28)], ct);
        if (validationId != null && !paid)
            return Result.Failure(ErrorCodes.PaymentReview, "Confirmation requires an order lookup.");
        var previous = order.Status;
        var ledger = paid ? await wallets.GetLedgerByKeyAsync($"order:{result.BankReference}", ct) : null;
        if (paid && order.CreditedAtUtc != null && order.ProviderTransactionId == result.BankReference &&
            ledger?.StartupId == order.StartupId && ledger.Reference == $"order:{order.OrderNumber}" &&
            ledger.Bucket == TokenBucket.Purchased && ledger.Amount == order.TokensSnapshot)
            order.Status = PaymentStatus.Paid;
        else if (order.CreditedAtUtc != null || result.Outcome is ProviderOutcome.Paid or ProviderOutcome.Review)
            order.Status = PaymentStatus.RequiresReview;
        else order.Status = result.Outcome == ProviderOutcome.Failed ? PaymentStatus.Failed : PaymentStatus.Pending;
        if (order.Status == PaymentStatus.RequiresReview && previous != order.Status)
            await activity.AuditAsync(Actor.System, "Payment needs finance review", orderNumber, "Provider settlement and wallet order require investigation.", ct: ct);
        order.UpdatedAtUtc = clock.UtcNow;
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Failure(ErrorCodes.PaymentUnavailable, "Order changed. Retry verification."); }
        return order.Status == PaymentStatus.Paid ? Result.Success("Payment already confirmed.") :
            Result.Failure(ErrorCodes.PaymentReview, $"Payment status is {order.Status}. No additional credits were issued.");
    }

    private async Task<Result> CreditAsync(PaymentOrder order, string txn, string payloadHash, string method, CancellationToken ct)
    {
        if (order.CreditedAtUtc is not null || await payments.ProviderTransactionUsedAsync(txn, ct) || await wallets.LedgerKeyExistsAsync($"order:{txn}", ct))
        {
            await payments.AddEventAsync(new PaymentProviderEvent { OrderId = order.Id, ProviderTransactionId = txn, SignatureValid = true, PayloadHash = payloadHash, Result = "Duplicate — ignored (already credited)" }, ct);
            await uow.SaveChangesAsync(ct);
            return Result.Failure(ErrorCodes.DuplicatePaymentEvent, "Duplicate confirmation ignored. Credits were added only once.");
        }
        var startup = await startups.GetByIdAsync(order.StartupId, ct);
        if (startup is null || startup.IsDeleted) return Result.Failure(ErrorCodes.PaymentReview, "The purchaser account is closed. Finance review is required.");
        startup.Version = Guid.NewGuid(); // serialize crediting with permanent account deletion
        var wallet = await wallets.GetByStartupAsync(order.StartupId, ct);
        if (wallet is null) { wallet = new StartupWallet { StartupId = order.StartupId }; await wallets.AddAsync(wallet, ct); }
        wallet.PurchasedBalance += order.TokensSnapshot;
        order.Status = PaymentStatus.Paid; order.ProviderTransactionId = txn; order.CreditedAtUtc = clock.UtcNow; order.Method = method;
        await wallets.AddLedgerAsync(new TokenLedgerEntry { StartupId = order.StartupId, Description = $"Credit purchase · {order.TokensSnapshot} credits (order {order.OrderNumber})", Bucket = TokenBucket.Purchased, Amount = order.TokensSnapshot, Reference = $"order:{order.OrderNumber}", IdempotencyKey = $"order:{txn}" }, ct);
        await payments.AddEventAsync(new PaymentProviderEvent { OrderId = order.Id, ProviderTransactionId = txn, SignatureValid = true, PayloadHash = payloadHash, Result = $"Credited {order.TokensSnapshot} Purchased" }, ct);
        await activity.NotifyAccountAsync(startup.OwnerAccountId, $"Receipt {order.OrderNumber}: BDT {order.PriceBdtSnapshot:0.00} received for {order.TokensSnapshot} RiseUp Credits.", "/wallet", ct);
        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure(ErrorCodes.PaymentUnavailable, "Wallet or order changed. Retry confirmation.");
        }
        return Result.Success($"Payment verified. {order.TokensSnapshot} purchased credits added.");
    }

    public async Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Actor actor, CancellationToken ct = default)
    {
        int? scope = actor.IsAdmin(AdminRole.FinanceAdmin) ? null : actor.IsFounder ? actor.StartupId : -1;
        if (scope == -1 || (actor.IsFounder && scope is null)) return [];
        return (await payments.ListOrdersAsync(scope, 100, ct))
            .Select(o => new OrderDto(o.OrderNumber, o.StartupId, o.TokensSnapshot, o.PriceBdtSnapshot, o.Status, o.ProviderTransactionId, o.CreditedAtUtc is not null, o.CreatedAtUtc)).ToList();
    }
}
