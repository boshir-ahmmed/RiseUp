using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

/// <summary>
/// DEMO/SANDBOX AI Model 2. Receives only permission-filtered profiles. Throws when an outage is simulated
/// so the Facade's automatic rule-based fallback (FR-061, FR-139) is exercised.
/// </summary>
public sealed class SandboxAiMatchingService(IOptions<AiOptions> options) : IAiMatchingService
{
    public Task<IReadOnlyList<MatchScore>> RankAsync(MatchProfile subject, IReadOnlyList<MatchProfile> candidates, CancellationToken ct = default)
    {
        if (options.Value.SimulateModel2Outage) throw new TimeoutException("AI Model 2 unavailable (simulated).");
        var subjectWords = Words(subject.PublicText);
        IReadOnlyList<MatchScore> result = candidates.Select(c =>
        {
            var reasons = new List<string>();
            var score = 0;
            var sectors = subject.Industries.Intersect(c.Industries, StringComparer.OrdinalIgnoreCase).ToList();
            if (sectors.Count > 0) { score += 40; reasons.Add($"Sector overlap: {string.Join(", ", sectors)}"); }
            var stages = subject.Stages.Intersect(c.Stages).ToList();
            if (stages.Count > 0) { score += 30; reasons.Add($"Stage fit: {string.Join(", ", stages)}"); }
            var shared = subjectWords.Intersect(Words(c.PublicText)).Take(3).ToList();
            if (shared.Count > 0) { score += 5 * shared.Count; reasons.Add($"Related themes: {string.Join(", ", shared)}"); }
            return new MatchScore(c.Id, Math.Min(98, score), reasons);
        }).OrderByDescending(s => s.Score).ToList();
        return Task.FromResult(result);
    }

    private static HashSet<string> Words(string? text) =>
        (text ?? string.Empty).ToLowerInvariant().Split([' ', ',', '.', '-', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 5).ToHashSet();
}
