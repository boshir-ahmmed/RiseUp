using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface IProfileRepository
{
    Task<MemberProfile?> GetForAccountAsync(int accountId, CancellationToken ct = default);
    Task<MemberProfile?> GetForTeamAsync(int teamAccountId, CancellationToken ct = default);
    Task AddAsync(MemberProfile profile, CancellationToken ct = default);
    /// <summary>accountId → photo media id, for avatars next to comments and messages.</summary>
    Task<Dictionary<int, int>> PhotoIdsForAccountsAsync(IReadOnlyCollection<int> accountIds, CancellationToken ct = default);
    Task<Dictionary<int, int>> PhotoIdsForTeamAsync(IReadOnlyCollection<int> teamAccountIds, CancellationToken ct = default);
    Task<NameChangeRequest?> GetNameRequestAsync(int id, CancellationToken ct = default);
    Task<NameChangeRequest?> GetPendingNameRequestAsync(int accountId, CancellationToken ct = default);
    Task AddNameRequestAsync(NameChangeRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<NameChangeRequest>> ListNameRequestsAsync(CancellationToken ct = default);
    /// <summary>Loads (tracked) every stored author-name copy for an account so an approved rename shows everywhere.</summary>
    Task<(IReadOnlyList<PostComment> Comments, IReadOnlyList<Message> Messages, IReadOnlyList<PitchQuestion> Questions, IReadOnlyList<Meeting> Meetings)>
        LoadAuthoredAsync(int? accountId, int? teamAccountId, CancellationToken ct = default);
}
