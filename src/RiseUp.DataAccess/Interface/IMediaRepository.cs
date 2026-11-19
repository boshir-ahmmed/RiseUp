using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface IMediaRepository : IRepository<MediaAsset>
{
    Task<IReadOnlyList<MediaAsset>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> ListPendingScanAsync(int take, CancellationToken ct = default);
}
