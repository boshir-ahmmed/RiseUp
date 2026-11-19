using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
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

namespace RiseUp.DataAccess.Repositories;

public sealed class PaymentRepository(RiseUpDbContext db) : Repository<PaymentOrder>(db), IPaymentRepository
{
    public async Task<IReadOnlyList<TokenPackage>> ListActivePackagesAsync(CancellationToken ct = default) =>
        await Db.TokenPackages.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Tokens).ToListAsync(ct);
    public Task<TokenPackage?> GetPackageAsync(int id, CancellationToken ct = default) => Db.TokenPackages.FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);
    public Task<PaymentOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default) => Set.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    public Task<bool> ProviderTransactionUsedAsync(string providerTransactionId, CancellationToken ct = default) =>
        Set.AnyAsync(o => o.ProviderTransactionId == providerTransactionId && o.CreditedAtUtc != null, ct);
    public async Task AddEventAsync(PaymentProviderEvent evt, CancellationToken ct = default) => await Db.PaymentProviderEvents.AddAsync(evt, ct);
    public async Task<IReadOnlyList<PaymentOrder>> ListOrdersAsync(int? startupId, int take, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(o => startupId == null || o.StartupId == startupId).OrderByDescending(o => o.CreatedAtUtc).Take(take).ToListAsync(ct);
    public async Task<IReadOnlyList<PromotionPackage>> ListPromotionPackagesAsync(CancellationToken ct = default) =>
        await Db.PromotionPackages.AsNoTracking().OrderBy(p => p.Days).ToListAsync(ct);
}
