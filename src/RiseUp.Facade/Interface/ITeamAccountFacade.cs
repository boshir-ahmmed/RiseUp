using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.TeamAccounts;

public interface ITeamAccountFacade
{
    Task<IReadOnlyList<TeamAccountDto>> ListAsync(Actor owner, CancellationToken ct = default);
    Task<Result<OneTimeCredential>> CreateAsync(Actor owner, CreateTeamAccountRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Actor owner, int id, CreateTeamAccountRequest request, CancellationToken ct = default);
    Task<Result<OneTimeCredential>> ResetPasswordAsync(Actor owner, int teamAccountId, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Actor owner, int teamAccountId, CancellationToken ct = default);
    Task<Result> ReactivateAsync(Actor owner, int teamAccountId, bool restorePreviousConversations, CancellationToken ct = default);
    Task<Result> DeleteAsync(Actor owner, int teamAccountId, CancellationToken ct = default);
}
