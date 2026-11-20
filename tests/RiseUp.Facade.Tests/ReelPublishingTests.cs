using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Facade.Media;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Promotions;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Tests;

public sealed class ReelPublishingTests
{
    [Fact]
    public async Task MovingTeaserMakesOnePrivateDraftAndPreservesPrivatePitch()
    {
        await using var host = await TestHost.CreateAsync();
        var founder = await host.Founder();
        var other = await host.SignIn("hello@takaledger.example");
        var package = (await host.Run<IPitchPackageFacade, Result<PitchPackageDto>>(p => p.GetOwnAsync(founder))).Value!;
        var videoId = package.Teaser!.MediaId;
        var moved = await host.Run<IPitchPackageFacade, Result<int>>(p => p.MoveTeaserToDraftAsync(founder));
        Assert.True(moved.IsSuccess, moved.Error.Message);
        Assert.True((await host.Run<IPitchPackageFacade, Result<int>>(p => p.MoveTeaserToDraftAsync(founder))).IsFailure);
        Assert.True((await host.Run<IMediaFacade, Result<MediaStream>>(m => m.OpenAsync(Actor.Anonymous, videoId, false))).IsFailure);

        var request = new SubmitPostRequest(moved.Value, PostCategory.Reel, "Loading bay", "Watch the crew in action.");
        Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(other, request))).IsFailure);
        Assert.True((await host.Run<IPromotionFacade, Result<int>>(p => p.PurchaseAsync(founder, moved.Value, 1))).IsFailure);
        var rejected = await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, request with { Body = "Guaranteed returns" }));
        Assert.Equal(ErrorCodes.ContentRejected, rejected.Error.Code);
        Assert.True((await host.Run<IMediaFacade, Result<MediaStream>>(m => m.OpenAsync(Actor.Anonymous, videoId, false))).IsFailure);

        Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, request))).IsSuccess);
        var publicVideo = await host.Run<IMediaFacade, Result<MediaStream>>(m => m.OpenAsync(Actor.Anonymous, videoId, false));
        Assert.True(publicVideo.IsSuccess);
        await publicVideo.Value!.Content.DisposeAsync();
        Assert.True((await host.Run<IMediaFacade, Result<MediaStream>>(m => m.OpenAsync(Actor.Anonymous, package.Pitch!.MediaId, false))).IsFailure);
    }

    [Fact]
    public async Task TwoReelsNeedDistinctCleanVideosAndHaveIndependentCampaigns()
    {
        await using var host = await TestHost.CreateAsync();
        var founder = await host.Founder();
        var ids = new List<int>();
        var request = new SubmitPostRequest(null, PostCategory.Reel, "Dispatch update", "Meet our freight team.");
        Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, request))).IsFailure);
        var balance = await host.Db(db => db.Wallets.Where(w => w.StartupId == founder.StartupId).Select(w => w.PurchasedBalance).SingleAsync());
        foreach (var title in new[] { "Day shift", "Night shift" })
        {
            var draft = await host.Run<IPostFacade, Result<int>>(p => p.SaveDraftAsync(founder, request with { Title = title }));
            Assert.True(draft.IsSuccess);
            var submission = request with { PostId = draft.Value, Title = title };
            Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, submission))).IsFailure);
            var video = await host.UploadAsync([1, 2, 3], "dispatch.mp4", "video/mp4", 12);
            Assert.True((await host.Run<IPostFacade, Result>(p => p.AttachVideoAsync(founder, draft.Value, video, null))).IsSuccess);
            await host.Db(async db =>
            {
                var asset = await db.MediaAssets.SingleAsync(m => m.StorageKey == video.StorageKey);
                asset.ScanStatus = ScanStatus.Pending;
                return await db.SaveChangesAsync();
            });
            Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, submission))).IsFailure);
            await host.Run<IMediaFacade, int>(m => m.ProcessScanQueueAsync());
            Assert.True((await host.Run<IPostFacade, Result<int>>(p => p.SubmitAsync(founder, submission))).IsSuccess);
            Assert.True((await host.Run<IPromotionFacade, Result<int>>(p => p.PurchaseAsync(founder, draft.Value, 1))).IsSuccess);
            Assert.True((await host.Run<IPromotionFacade, Result<int>>(p => p.PurchaseAsync(founder, draft.Value, 1))).IsFailure);
            ids.Add(draft.Value);
        }
        var feed = await host.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(p => p.GetFeedAsync(Actor.Anonymous, "foryou", PostCategory.Reel, 20));
        Assert.Equal(2, feed.Count(p => ids.Contains(p.Id) && p.IsPromoted));
        Assert.Equal(balance - 6, await host.Db(db => db.Wallets.Where(w => w.StartupId == founder.StartupId).Select(w => w.PurchasedBalance).SingleAsync()));
    }
}
