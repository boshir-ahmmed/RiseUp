using RiseUp.Entities.Common;

namespace RiseUp.Entities.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Serializable transaction for race-sensitive operations (debits, quotas, accept).</summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
