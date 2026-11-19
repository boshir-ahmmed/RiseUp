using RiseUp.Entities.Common;

namespace RiseUp.Entities.Abstractions;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    void Remove(TEntity entity);
}
