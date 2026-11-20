using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Discovery;


/// <summary>
/// AI Model 2 with a bounded timeout and automatic deterministic fallback. Only profile fields the requester
/// may see are sent; IDs are internal, and no emails, phones, documents or chats are ever included (FR-059, NFR-23).
/// AI output is validated against the permission-filtered candidate set and cannot authorise anything (FR-060).
/// </summary>
public sealed class MatchingFacade(IStartupRepository startups, IInvestorRepository investors, IConnectionRepository connections,
    IProfileRepository profiles, IAiMatchingService ai, ILogger<MatchingFacade> logger, RiseUp.Framework.Time.IClock clock) : IMatchingFacade
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    private static string StageLabel(Entities.Common.FundingStage s) => s switch
    {
        Entities.Common.FundingStage.PreSeed => "Pre-seed", Entities.Common.FundingStage.SeriesA => "Series A", Entities.Common.FundingStage.SeriesB => "Series B", _ => s.ToString(),
    };

    public static MatchProfile ToProfile(Startup s) =>
        new(s.Id, [s.Industry], [s.Stage], null, s.FundingGoalUsd, s.Location, Sanitize(s.Tagline));
    public static MatchProfile ToProfile(InvestorProfile i, bool includeRange) =>
        new(i.Id, i.Industries.ToList(), i.Stages.ToList(), includeRange ? i.TicketMinUsd : null, includeRange ? i.TicketMaxUsd : null, i.Location, Sanitize(i.Bio));

    /// <summary>Profile text is untrusted: strip control characters and cap length to blunt prompt injection.</summary>
    public static string Sanitize(string? text) =>
        new string((text ?? "").Where(c => !char.IsControl(c)).Take(280).ToArray()).Replace("{", "(").Replace("}", ")");

    public async Task<MatchList> RecommendStartupsForInvestorAsync(Actor actor, int take, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return new MatchList("none", false, []);
        var me = await investors.GetByIdAsync(actor.InvestorId!.Value, ct);
        if (me is null || !me.IsActiveVerifiedAt(clock.UtcNow)) return new MatchList("none", false, []);
        var pool = new List<Startup>();
        foreach (var s in await startups.ListActiveVerifiedAsync(500, ct))
            if (await connections.GetConnectionAsync(s.Id, me.Id, ct) is null && await connections.GetBlockAsync(s.Id, me.Id, ct) is null) pool.Add(s);
        var (scores, engine, fallback) = await RankAsync(ToProfile(me, includeRange: true), pool.Select(ToProfile).ToList(), ct);
        var byId = pool.ToDictionary(s => s.Id);
        return new MatchList(engine, fallback, scores.Where(s => byId.ContainsKey(s.CandidateId)).Take(take)
            .Select(s => { var x = byId[s.CandidateId]; return new MatchItem(x.Id, x.Name, $"{x.Industry} · {StageLabel(x.Stage)}", s.Score, s.Reasons, x.ColorIndex, x.LogoMediaId is int logo ? RiseUp.Framework.Constants.UrlRewrite.Media(logo) : null); }).ToList());
    }

    public async Task<MatchList> RecommendInvestorsForStartupAsync(Actor actor, int take, CancellationToken ct = default)
    {
        if (!actor.IsFounder || actor.StartupId is null) return new MatchList("none", false, []);
        var me = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (me is null) return new MatchList("none", false, []);
        var includeRange = me.IsActiveVerifiedAt(clock.UtcNow);   // FR-039: ranges visible to verified startups only
        var pool = new List<InvestorProfile>();
        foreach (var i in await investors.ListActiveVerifiedAsync(500, ct))
            if (await connections.GetBlockAsync(me.Id, i.Id, ct) is null) pool.Add(i);
        var (scores, engine, fallback) = await RankAsync(ToProfile(me), pool.Select(i => ToProfile(i, includeRange)).ToList(), ct);
        var byId = pool.ToDictionary(i => i.Id);
        var ranked = scores.Where(s => byId.ContainsKey(s.CandidateId)).Take(take).ToList();
        var photos = await profiles.PhotoIdsForAccountsAsync(ranked.Select(s => byId[s.CandidateId].AccountId).ToList(), ct);
        return new MatchList(engine, fallback, ranked.Select(s =>
        {
            var i = byId[s.CandidateId];
            return new MatchItem(i.Id, i.DisplayName, i.Affiliation, s.Score, s.Reasons, i.ColorIndex,
                photos.TryGetValue(i.AccountId, out var photo) ? RiseUp.Framework.Constants.UrlRewrite.Media(photo) : null);
        }).ToList());
    }

    private async Task<(IReadOnlyList<MatchScore> Scores, string Engine, bool Fallback)> RankAsync(MatchProfile subject, IReadOnlyList<MatchProfile> candidates, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            var allowed = candidates.Select(c => c.Id).ToHashSet();
            var scores = (await ai.RankAsync(subject, candidates, cts.Token)).Where(s => allowed.Contains(s.CandidateId)).ToList();
            return (scores, "AI Model 2", false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogWarning("AI Model 2 unavailable ({Reason}); using rule-based matching.", ex.GetType().Name);
            return (RuleBasedMatcher.Rank(subject, candidates), "Rule-based fallback", true);
        }
    }
}
