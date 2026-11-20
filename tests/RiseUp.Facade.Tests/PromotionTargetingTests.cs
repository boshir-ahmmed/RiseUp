using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Posts;

namespace RiseUp.Facade.Tests;

public class PromotionTargetingTests
{
    [Fact]
    public async Task OlderPaidPostStaysEligibleAndOnlyReturnedPlacementsCount()
    {
        await using var host = await TestHost.CreateAsync();
        var investor = await host.Investor();
        var campaign = await host.Db(db => db.Promotions.SingleAsync(p => p.Status == PromotionStatus.Active));
        var otherCategory = await host.Db(async db =>
        {
            var category = (await db.Posts.SingleAsync(p => p.Id == campaign.PostId)).Category;
            var other = Enum.GetValues<PostCategory>().First(c => c != category);
            for (var i = 0; i < 201; i++)
                db.Posts.Add(new() { StartupId = campaign.StartupId, LiveTitle = $"Update {i}", LiveBody = "Update", Category = other, PublishedAtUtc = host.Clock.UtcNow });
            await db.SaveChangesAsync();
            return other;
        });
        Task<IReadOnlyList<FeedItemDto>> Feed(string tab, PostCategory? category = null) =>
            host.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(p => p.GetFeedAsync(investor, tab, category, 1));

        Assert.False(Assert.Single(await Feed("latest")).IsPromoted);
        Assert.False(Assert.Single(await Feed("foryou", otherCategory)).IsPromoted);
        Assert.Equal(campaign.PostId, Assert.Single(await Feed("foryou")).Id);
        var after = await host.Db(db => db.Promotions.SingleAsync(p => p.Id == campaign.Id));
        Assert.Equal(campaign.Impressions + 1, after.Impressions);
        Assert.Equal(campaign.FallbackImpressions, after.FallbackImpressions);
    }

    [Fact]
    public async Task FeedPreservesVisibilityAndRecoversAfterDuplicateScores()
    {
        var model = new RecordingMatcher();
        await using var host = await TestHost.CreateAsync(configure: services => services.AddSingleton<IAiMatchingService>(model));
        var investor = await host.Investor();
        await host.Db(async db =>
        {
            var profile = await db.InvestorProfiles.SingleAsync(i => i.Id == investor.InvestorId);
            profile.VerificationStatus = VerificationStatus.Pending;
            return await db.SaveChangesAsync();
        });
        var before = await host.Db(db => db.Promotions.SingleAsync(p => p.Status == PromotionStatus.Active));
        Task<IReadOnlyList<FeedItemDto>> Feed(string tab) =>
            host.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(p => p.GetFeedAsync(investor, tab, null, 50));

        Assert.Contains(await Feed("foryou"), p => p.Id == before.PostId && p.IsPromoted);
        Assert.NotEmpty(model.Candidates);
        Assert.All(model.Candidates, p => Assert.Null(p.MaxUsd));
        model.Duplicate = false;
        Assert.Contains(await Feed("foryou"), p => p.Id == before.PostId && p.IsPromoted);
        Assert.Contains(await Feed("latest"), p => p.Id == before.PostId && p.IsPromoted);
        var after = await host.Db(db => db.Promotions.SingleAsync(p => p.Id == before.Id));
        Assert.Equal(before.Impressions + 3, after.Impressions);
        Assert.Equal(before.FallbackImpressions + 1, after.FallbackImpressions);
        Assert.Equal(before.RemainingSeconds, after.RemainingSeconds);
    }

    [Fact]
    public async Task ModelPreferenceOrdersPaidPlacementsAheadOfOrganicPosts()
    {
        var model = new RecordingMatcher { Duplicate = false };
        await using var host = await TestHost.CreateAsync(configure: services => services.AddSingleton<IAiMatchingService>(model));
        var investor = await host.Investor();
        var first = await host.Db(db => db.Promotions.SingleAsync(p => p.Status == PromotionStatus.Active));
        var second = await host.Db(async db =>
        {
            var post = await db.Posts.FirstAsync(p => p.StartupId != first.StartupId && p.LiveTitle != null && !p.IsHidden);
            db.Promotions.Add(new() { PostId = post.Id, StartupId = post.StartupId, Status = PromotionStatus.Active,
                Days = 1, RemainingSeconds = 86400, StartedAtUtc = host.Clock.UtcNow, LastTickUtc = host.Clock.UtcNow });
            await db.SaveChangesAsync();
            return post;
        });
        Task<IReadOnlyList<FeedItemDto>> Feed() =>
            host.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(p => p.GetFeedAsync(investor, "foryou", null, 10));
        model.Favorite = second.StartupId;
        var page = await Feed();
        Assert.Equal(second.Id, page[0].Id);
        Assert.Equal(first.PostId, page[1].Id);
        model.Favorite = first.StartupId;
        Assert.Equal(first.PostId, (await Feed())[0].Id);
    }

    private sealed class RecordingMatcher : IAiMatchingService
    {
        public int? Favorite { get; set; }
        public bool Duplicate { get; set; } = true;
        public IReadOnlyList<MatchProfile> Candidates { get; private set; } = [];
        public Task<IReadOnlyList<MatchScore>> RankAsync(MatchProfile subject, IReadOnlyList<MatchProfile> candidates, CancellationToken ct)
        {
            Candidates = candidates;
            var scores = candidates.Select(p => new MatchScore(p.Id, p.Id == Favorite ? 100 : 0, ["Stage match"])).ToList();
            if (Duplicate) scores.Add(scores[0]);
            return Task.FromResult<IReadOnlyList<MatchScore>>(scores);
        }
    }
}
