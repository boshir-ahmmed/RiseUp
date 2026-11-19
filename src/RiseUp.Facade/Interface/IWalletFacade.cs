using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Wallets;

public interface IWalletFacade
{
    Task<Result<WalletDto>> GetAsync(Actor actor, CancellationToken ct = default);
    Task<Result> AdjustAsync(Actor admin, int startupId, TokenBucket bucket, int amount, string reason, CancellationToken ct = default);
}
