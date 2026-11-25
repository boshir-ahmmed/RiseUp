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

public sealed class TeamAccountRepository(RiseUpDbContext db) : Repository<TeamAccount>(db), ITeamAccountRepository
{
    public async Task<IReadOnlyList<TeamAccount>> ListForStartupAsync(int startupId, bool readOnly, CancellationToken ct = default) =>
        await (readOnly ? Set.AsNoTracking() : Set).Where(t => t.StartupId == startupId && t.Status != TeamAccountStatus.Deleted).OrderBy(t => t.Id).ToListAsync(ct);
    public Task<bool> HasActiveSessionAsync(int id, int version, CancellationToken ct = default) =>
        Set.AnyAsync(t => t.Id == id && t.SessionVersion == version && t.Status == TeamAccountStatus.Active, ct);
    public Task<int> CountOccupyingSlotsAsync(int startupId, CancellationToken ct = default) =>
        Set.CountAsync(t => t.StartupId == startupId && t.Status != TeamAccountStatus.Deleted, ct);
    public Task<bool> UsernameOrEmailTakenAsync(string username, string email, CancellationToken ct = default) =>
        Set.AnyAsync(t => t.Username == username || t.Email == email, ct);
    public Task<TeamAccount?> FindByLoginAsync(string identifier, CancellationToken ct = default)
    {
        var members = Set.Where(t => t.Status != TeamAccountStatus.Deleted);
        if (!identifier.Contains('@')) return members.FirstOrDefaultAsync(t => t.Username == identifier, ct);
        var normalizedEmail = identifier.ToUpperInvariant();
        return members.FirstOrDefaultAsync(t => t.Email.Trim().ToUpper() == normalizedEmail, ct);
    }
}
