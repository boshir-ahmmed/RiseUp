using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Repositories;

public sealed class ProfileRepository(RiseUpDbContext db) : IProfileRepository
{
    public Task<MemberProfile?> GetForAccountAsync(int accountId, CancellationToken ct = default) => db.MemberProfiles.FirstOrDefaultAsync(p => p.AccountId == accountId, ct);
    public Task<MemberProfile?> GetForTeamAsync(int teamAccountId, CancellationToken ct = default) => db.MemberProfiles.FirstOrDefaultAsync(p => p.TeamAccountId == teamAccountId, ct);
    public async Task AddAsync(MemberProfile profile, CancellationToken ct = default) => await db.MemberProfiles.AddAsync(profile, ct);

    public Task<Dictionary<int, int>> PhotoIdsForAccountsAsync(IReadOnlyCollection<int> accountIds, CancellationToken ct = default) =>
        db.MemberProfiles.Where(p => p.AccountId != null && accountIds.Contains(p.AccountId.Value) && p.PhotoMediaId != null)
            .ToDictionaryAsync(p => p.AccountId!.Value, p => p.PhotoMediaId!.Value, ct);
    public Task<Dictionary<int, int>> PhotoIdsForTeamAsync(IReadOnlyCollection<int> teamAccountIds, CancellationToken ct = default) =>
        db.MemberProfiles.Where(p => p.TeamAccountId != null && teamAccountIds.Contains(p.TeamAccountId.Value) && p.PhotoMediaId != null)
            .ToDictionaryAsync(p => p.TeamAccountId!.Value, p => p.PhotoMediaId!.Value, ct);

    public Task<NameChangeRequest?> GetNameRequestAsync(int id, CancellationToken ct = default) => db.NameChangeRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
    public Task<NameChangeRequest?> GetPendingNameRequestAsync(int accountId, CancellationToken ct = default) =>
        db.NameChangeRequests.FirstOrDefaultAsync(r => r.AccountId == accountId && r.Status == NameChangeStatus.Pending, ct);
    public async Task AddNameRequestAsync(NameChangeRequest request, CancellationToken ct = default) => await db.NameChangeRequests.AddAsync(request, ct);
    public async Task<IReadOnlyList<NameChangeRequest>> ListNameRequestsAsync(CancellationToken ct = default) =>
        await db.NameChangeRequests.AsNoTracking().OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAtUtc).Take(200).ToListAsync(ct);

    public async Task<(IReadOnlyList<PostComment>, IReadOnlyList<Message>, IReadOnlyList<PitchQuestion>, IReadOnlyList<Meeting>)> LoadAuthoredAsync(int? accountId, int? teamAccountId, CancellationToken ct = default)
    {
        var comments = accountId is int a ? await db.PostComments.Where(c => c.AuthorAccountId == a).ToListAsync(ct) : [];
        var messages = await db.Messages.Where(m => (accountId != null && m.AuthorAccountId == accountId) || (teamAccountId != null && m.AuthorTeamAccountId == teamAccountId)).ToListAsync(ct);
        var questions = accountId is int q ? await db.PitchQuestions.Where(x => x.AskedByAccountId == q).ToListAsync(ct) : [];
        var meetings = accountId is int m2 ? await db.Meetings.Where(x => x.ProposedByAccountId == m2).ToListAsync(ct) : [];
        return (comments, messages, questions, meetings);
    }
}
