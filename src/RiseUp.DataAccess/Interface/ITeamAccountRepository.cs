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

namespace RiseUp.Entities.Abstractions;

public interface ITeamAccountRepository : IRepository<TeamAccount>
{
    Task<IReadOnlyList<TeamAccount>> ListForStartupAsync(int startupId, CancellationToken ct = default);
    Task<int> CountOccupyingSlotsAsync(int startupId, CancellationToken ct = default);
    Task<bool> UsernameOrEmailTakenAsync(string username, string email, CancellationToken ct = default);
    Task<TeamAccount?> FindByUsernameAsync(string username, CancellationToken ct = default);
}
