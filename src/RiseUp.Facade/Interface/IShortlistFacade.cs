using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Pitching;

public interface IShortlistFacade
{
    Task<IReadOnlyList<ShortlistItemDto>> ListAsync(Actor actor, CancellationToken ct = default);
    Task<Result<bool>> ToggleAsync(Actor actor, int startupId, CancellationToken ct = default);
    Task<Result> UpdateAsync(Actor actor, int startupId, ShortlistStage stage, string? note, CancellationToken ct = default);
}
