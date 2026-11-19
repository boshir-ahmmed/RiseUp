using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Investors;

public interface IInvestorFacade
{
    Task<IReadOnlyList<InvestorCardDto>> ListAsync(Actor actor, CancellationToken ct = default);
    Task<Result<InvestorDetailDto>> GetDetailAsync(Actor actor, int id, CancellationToken ct = default);
    Task<Result> SetReceivePitchesAsync(Actor actor, bool receive, CancellationToken ct = default);
}
