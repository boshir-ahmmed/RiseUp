using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Startups;

public interface IStartupFacade
{
    Task<PagedResult<StartupCardDto>> SearchAsync(Actor actor, SearchStartupsRequest request, CancellationToken ct = default);
    Task<Result<StartupDetailDto>> GetDetailAsync(Actor actor, int id, string viewerKey, CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(Actor actor, UpdateStartupRequest request, CancellationToken ct = default);
    Task<Result> SuspendAsync(Actor moderator, int startupId, string reason, CancellationToken ct = default);
    Task<Result> LiftSuspensionAsync(Actor moderator, int startupId, string reason, CancellationToken ct = default);
}
