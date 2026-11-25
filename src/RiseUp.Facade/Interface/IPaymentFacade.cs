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

public interface IPaymentFacade
{
    Task<IReadOnlyList<PackageDto>> ListPackagesAsync(CancellationToken ct = default);
    PaymentMode Mode { get; }
    Task<Result<CheckoutDto>> CreateOrderAsync(Actor actor, int packageId, BillingAddress? billing = null, CancellationToken ct = default);
    Task<OrderDto?> GetSandboxOrderAsync(Actor actor, string orderNumber, CancellationToken ct = default);
    Task<Result> CompleteSandboxAsync(Actor actor, string orderNumber, string? method, string? outcome, CancellationToken ct = default);
    Task<Result> ConfirmHostedAsync(string orderNumber, string? validationId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> DueForReconciliationAsync(CancellationToken ct = default);
    /// <summary>Server-to-server confirmation. Payload: "orderNumber|providerTransactionId|status". Idempotent.</summary>
    Task<Result> HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
    Task<Result> ReconcileAsync(Actor admin, string orderNumber, string providerTransactionId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Actor actor, CancellationToken ct = default);
}
