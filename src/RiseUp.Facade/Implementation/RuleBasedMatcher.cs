using RiseUp.Entities.Abstractions;

namespace RiseUp.Facade.Discovery;

/// <summary>
/// Deterministic fallback (FR-061, NFR-21): industry, stage, funding range and geography only.
/// Explanations describe overlap; they never claim funding is likely or guaranteed (FR-058).
/// </summary>
public static class RuleBasedMatcher
{
    public static IReadOnlyList<MatchScore> Rank(MatchProfile subject, IReadOnlyList<MatchProfile> candidates) =>
        candidates.Select(c => Score(subject, c)).OrderByDescending(s => s.Score).ThenBy(s => s.CandidateId).ToList();

    public static MatchScore Score(MatchProfile a, MatchProfile b)
    {
        var reasons = new List<string>();
        var score = 0;
        var sectors = a.Industries.Intersect(b.Industries, StringComparer.OrdinalIgnoreCase).ToList();
        if (sectors.Count > 0) { score += 40; reasons.Add($"Sector match: {string.Join(", ", sectors)}"); }
        var stages = a.Stages.Intersect(b.Stages).ToList();
        if (stages.Count > 0) { score += 30; reasons.Add($"Stage fit: {string.Join(", ", stages)}"); }
        var (min, max) = (a.MinUsd ?? b.MinUsd, a.MaxUsd ?? b.MaxUsd);
        var goal = a.MinUsd is null ? a.MaxUsd : b.MinUsd is null ? b.MaxUsd : null;
        if (min is not null && max is not null && goal is not null && max >= goal * 0.1m && min <= goal)
        { score += 20; reasons.Add($"Ticket range ${min:N0}–${max:N0} fits a ${goal:N0} raise"); }
        if (!string.IsNullOrEmpty(a.Location) && !string.IsNullOrEmpty(b.Location) &&
            (a.Location.Contains(b.Location, StringComparison.OrdinalIgnoreCase) || b.Location.Contains(a.Location, StringComparison.OrdinalIgnoreCase)))
        { score += 10; reasons.Add($"Same region: {b.Location}"); }
        if (reasons.Count == 0) reasons.Add("Limited overlap — shown for discovery");
        return new MatchScore(b.Id, Math.Min(98, score), reasons);
    }
}
