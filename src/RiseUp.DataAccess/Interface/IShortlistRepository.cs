using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface IShortlistRepository
{
    Task<ShortlistEntry?> GetAsync(int investorId, int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<ShortlistEntry>> ListAsync(int investorId, CancellationToken ct = default);
    Task AddAsync(ShortlistEntry entry, CancellationToken ct = default);
    void Remove(ShortlistEntry entry);
}
