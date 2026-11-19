using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Repositories;

public sealed class MediaRepository(RiseUpDbContext db) : Repository<MediaAsset>(db), IMediaRepository
{
    public override Task<MediaAsset?> GetByIdAsync(int id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(m => m.Id == id && m.DeletedAtUtc == null, ct);
    public async Task<IReadOnlyList<MediaAsset>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(m => ids.Contains(m.Id) && m.DeletedAtUtc == null).ToListAsync(ct);
    public async Task<IReadOnlyList<MediaAsset>> ListPendingScanAsync(int take, CancellationToken ct = default) =>
        await Set.Where(m => m.ScanStatus == ScanStatus.Pending && m.DeletedAtUtc == null).OrderBy(m => m.Id).Take(take).ToListAsync(ct);
}
