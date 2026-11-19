using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Media;

public interface IMediaFacade
{
    /// <summary>Re-authorises every request. Unknown, quarantined and forbidden media all return not-found (no existence oracle).</summary>
    Task<Result<MediaStream>> OpenAsync(Actor actor, int mediaId, bool download, CancellationToken ct = default);
    Task<int> ProcessScanQueueAsync(CancellationToken ct = default);
}
