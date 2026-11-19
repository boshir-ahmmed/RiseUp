using RiseUp.Entities.Common;

namespace RiseUp.Entities.Abstractions;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
