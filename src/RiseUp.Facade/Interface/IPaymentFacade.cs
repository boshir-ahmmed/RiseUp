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
    Task<Result<CheckoutDto>> CreateOrderAsync(Actor actor, int packageId, CancellationToken ct = default);
    /// <summary>Server-to-server confirmation. Payload: "orderNumber|providerTransactionId|status". Idempotent.</summary>
    Task<Result> HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
    Task<Result> ReconcileAsync(Actor admin, string orderNumber, string providerTransactionId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Actor actor, CancellationToken ct = default);
}
