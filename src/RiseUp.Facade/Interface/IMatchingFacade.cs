using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Discovery;

public interface IMatchingFacade
{
    Task<MatchList> RecommendStartupsForInvestorAsync(Actor actor, int take, CancellationToken ct = default);
    Task<MatchList> RecommendInvestorsForStartupAsync(Actor actor, int take, CancellationToken ct = default);
}
