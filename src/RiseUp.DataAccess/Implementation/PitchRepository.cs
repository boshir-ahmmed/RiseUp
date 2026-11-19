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

public sealed class PitchRepository(RiseUpDbContext db) : IPitchRepository
{
    private IQueryable<PitchPackage> Full => db.PitchPackages.Include(p => p.Slides).Include(p => p.UseOfFunds).Include(p => p.Team).Include(p => p.Faq).Include(p => p.Documents).AsSplitQuery();

    public Task<PitchPackage?> GetPackageAsync(int startupId, CancellationToken ct = default) => Full.FirstOrDefaultAsync(p => p.StartupId == startupId, ct);
    public async Task<IReadOnlyList<PitchPackage>> ListPackagesAsync(IReadOnlyCollection<int> startupIds, CancellationToken ct = default) =>
        await Full.AsNoTracking().Where(p => startupIds.Contains(p.StartupId)).ToListAsync(ct);
    public async Task AddPackageAsync(PitchPackage package, CancellationToken ct = default) => await db.PitchPackages.AddAsync(package, ct);
    public Task<PitchPackage?> FindPackageByMediaAsync(int mediaId, CancellationToken ct = default) =>
        db.PitchPackages.AsNoTracking().FirstOrDefaultAsync(p => p.PitchVideoId == mediaId || p.DemoVideoId == mediaId || p.TeaserVideoId == mediaId
            || p.PitchPosterId == mediaId || p.DemoPosterId == mediaId || p.TeaserPosterId == mediaId || p.DeckMediaId == mediaId, ct);
    public Task<PitchDocument?> FindDocumentByMediaAsync(int mediaId, CancellationToken ct = default) => db.PitchDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.MediaId == mediaId, ct);
    public async Task<IReadOnlyList<ConnectionRequest>> ListPitchesForPairAsync(int startupId, int investorId, CancellationToken ct = default) =>
        await db.ConnectionRequests.AsNoTracking().Where(r => r.StartupId == startupId && r.InvestorId == investorId && r.Type == RequestType.Pitch).ToListAsync(ct);
    public async Task<IReadOnlyList<PitchQuestion>> ListQuestionsAsync(int requestId, CancellationToken ct = default) =>
        await db.PitchQuestions.AsNoTracking().Where(q => q.RequestId == requestId).OrderBy(q => q.CreatedAtUtc).ThenBy(q => q.Id).ToListAsync(ct);
    public async Task<Dictionary<int, (int Total, int Unanswered)>> CountQuestionsAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct = default) =>
        (await db.PitchQuestions.Where(q => requestIds.Contains(q.RequestId)).GroupBy(q => q.RequestId)
            .Select(g => new { g.Key, Total = g.Count(), Open = g.Count(x => x.Answer == null) }).ToListAsync(ct))
        .ToDictionary(x => x.Key, x => (x.Total, x.Open));
    public Task<PitchQuestion?> GetQuestionAsync(int id, CancellationToken ct = default) => db.PitchQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
    public async Task AddQuestionAsync(PitchQuestion question, CancellationToken ct = default) => await db.PitchQuestions.AddAsync(question, ct);
    public Task<PitchEngagement?> GetEngagementAsync(int requestId, CancellationToken ct = default) => db.PitchEngagements.FirstOrDefaultAsync(e => e.RequestId == requestId, ct);
    public Task<Dictionary<int, PitchEngagement>> ListEngagementsAsync(IReadOnlyCollection<int> requestIds, CancellationToken ct = default) =>
        db.PitchEngagements.AsNoTracking().Where(e => requestIds.Contains(e.RequestId)).ToDictionaryAsync(e => e.RequestId, ct);
    public async Task AddEngagementAsync(PitchEngagement engagement, CancellationToken ct = default) => await db.PitchEngagements.AddAsync(engagement, ct);
}
