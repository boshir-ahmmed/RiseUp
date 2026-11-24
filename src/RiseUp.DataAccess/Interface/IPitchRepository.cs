using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.Entities.Abstractions;

public interface IPitchRepository
{
    Task<PitchPackage?> GetPackageAsync(int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<PitchPackage>> ListPackagesAsync(IReadOnlyCollection<int> startupIds, CancellationToken ct = default);
    Task AddPackageAsync(PitchPackage package, CancellationToken ct = default);
    Task<PitchPackage?> FindPackageByMediaAsync(int mediaId, CancellationToken ct = default);
    Task<PitchDocument?> FindDocumentByMediaAsync(int mediaId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequest>> ListPitchesForPairAsync(int startupId, int investorId, CancellationToken ct = default);
    Task<IReadOnlyList<PitchQuestion>> ListQuestionsAsync(int requestId, CancellationToken ct = default);
    Task<Dictionary<int, (int Total, int Unanswered)>> CountQuestionsAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct = default);
    Task<IReadOnlyList<PitchInfoItem>> ListInfoItemsAsync(int requestId, CancellationToken ct);
    Task<PitchInfoItem?> GetInfoItemAsync(int requestId, int itemId, CancellationToken ct);
    Task<List<int>> ListPitchesAwaitingInfoAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct);
    Task AddInfoItemsAsync(IEnumerable<PitchInfoItem> items, CancellationToken ct);
    Task<ConnectionRequest?> FindInfoAttachmentRequestAsync(int mediaId, CancellationToken ct);
    Task<PitchQuestion?> GetQuestionAsync(int id, CancellationToken ct = default);
    Task AddQuestionAsync(PitchQuestion question, CancellationToken ct = default);
    Task<PitchEngagement?> GetEngagementAsync(int requestId, CancellationToken ct = default);
    Task<Dictionary<int, PitchEngagement>> ListEngagementsAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct = default);
    Task AddEngagementAsync(PitchEngagement engagement, CancellationToken ct = default);
}
