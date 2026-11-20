using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;

namespace RiseUp.Facade.Discovery;

public sealed class MatchRanker(IAiMatchingService ai, ILogger<MatchRanker> logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    public async Task<(IReadOnlyList<MatchScore> Scores, string Engine, bool Fallback)> RankAsync(MatchProfile subject, IReadOnlyList<MatchProfile> candidates, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (candidates.Count == 0) return ([], "AI Model 2", false);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            var allowed = candidates.Select(c => c.Id).ToHashSet();
            var response = await ai.RankAsync(subject, candidates, cts.Token).WaitAsync(cts.Token);
            var scores = response.Where(s => allowed.Contains(s.CandidateId)).ToList();
            if (scores.Count == 0 || scores.Any(s => s.Score is < 0 or > 100 || s.Reasons is null)
                || scores.DistinctBy(s => s.CandidateId).Count() != scores.Count)
                throw new InvalidOperationException("Unusable matching scores.");
            return (scores, "AI Model 2", false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("AI Model 2 unavailable ({Reason}); using rule-based matching.", ex.GetType().Name);
            return (RuleBasedMatcher.Rank(subject, candidates), "Rule-based fallback", true);
        }
    }
}
