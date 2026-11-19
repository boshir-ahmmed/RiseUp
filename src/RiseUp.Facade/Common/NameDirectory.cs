using RiseUp.Entities.Abstractions;

namespace RiseUp.Facade.Common;

/// <summary>
/// Resolves an actor's CURRENT display name from the database. Sign-in cookies carry the name at sign-in time, so
/// anything that stores an author name (comments, messages, questions, meetings) uses this to stay correct after
/// an approved name change.
/// </summary>
public sealed class NameDirectory(IUserAccountRepository accounts, IInvestorRepository investors, ITeamAccountRepository team)
{
    public async Task<string> CurrentNameAsync(Actor actor, CancellationToken ct = default)
    {
        if (actor.IsInvestor && await investors.GetByIdAsync(actor.InvestorId!.Value, ct) is { } i) return i.DisplayName;
        if (actor.IsTeamMember && await team.GetByIdAsync(actor.TeamAccountId!.Value, ct) is { } t) return t.FullName;
        if (actor.AccountId is int a && await accounts.GetByIdAsync(a, ct) is { } u) return u.FullName;
        return actor.Name;
    }
}
