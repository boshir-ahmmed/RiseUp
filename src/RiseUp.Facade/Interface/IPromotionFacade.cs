using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Promotions;

public interface IPromotionFacade
{
    Task<Result<int>> PurchaseAsync(Actor actor, int postId, int days, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionDto>> ListForStartupAsync(Actor actor, CancellationToken ct = default);
    /// <summary>Background job: advances paid clocks only for eligible startups; pauses the rest with time preserved.</summary>
    Task<int> RunClockAsync(CancellationToken ct = default);
    Task PauseForStartupAsync(int startupId, string reason, CancellationToken ct = default);
    Task ResumeForStartupAsync(int startupId, CancellationToken ct = default);
    Task EndAllForStartupAsync(int startupId, CancellationToken ct = default);
}
