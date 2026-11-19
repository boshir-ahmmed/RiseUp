using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Governance;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;

namespace RiseUp.Entities.Abstractions;

public interface IPaymentRepository : IRepository<PaymentOrder>
{
    Task<IReadOnlyList<TokenPackage>> ListActivePackagesAsync(CancellationToken ct = default);
    Task<TokenPackage?> GetPackageAsync(int id, CancellationToken ct = default);
    Task<PaymentOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<bool> ProviderTransactionUsedAsync(string providerTransactionId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListDueOrdersAsync(string prefix, DateTime now, CancellationToken ct);
    Task AddEventAsync(PaymentProviderEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentOrder>> ListOrdersAsync(int? startupId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionPackage>> ListPromotionPackagesAsync(CancellationToken ct = default);
}
