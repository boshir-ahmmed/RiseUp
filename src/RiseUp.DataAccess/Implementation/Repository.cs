using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;

namespace RiseUp.DataAccess.Repositories;

/// <summary>Generic repository. Deliberately has no SaveChanges — see <see cref="UnitOfWork"/>.</summary>
public class Repository<TEntity>(RiseUpDbContext db) : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly RiseUpDbContext Db = db;
    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(e => e.Id == id, ct);
    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
