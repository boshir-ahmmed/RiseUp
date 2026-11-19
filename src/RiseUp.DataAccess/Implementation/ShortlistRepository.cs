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

public sealed class ShortlistRepository(RiseUpDbContext db) : IShortlistRepository
{
    public Task<ShortlistEntry?> GetAsync(int investorId, int startupId, CancellationToken ct = default) => db.Shortlist.FirstOrDefaultAsync(s => s.InvestorId == investorId && s.StartupId == startupId, ct);
    public async Task<IReadOnlyList<ShortlistEntry>> ListAsync(int investorId, CancellationToken ct = default) =>
        await db.Shortlist.AsNoTracking().Where(s => s.InvestorId == investorId).OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc).ToListAsync(ct);
    public async Task AddAsync(ShortlistEntry entry, CancellationToken ct = default) => await db.Shortlist.AddAsync(entry, ct);
    public void Remove(ShortlistEntry entry) => db.Shortlist.Remove(entry);
}
