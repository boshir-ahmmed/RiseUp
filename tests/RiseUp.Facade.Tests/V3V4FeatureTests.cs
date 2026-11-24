using System.Text;
using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Media;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Profiles;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Tests;

/// <summary>v3 (video pitching) and v4 (profiles, shell) business rules — see docs/V3_PITCHING_SPEC.md and docs/V4_PROFILE_AND_UI_SPEC.md.</summary>
public class V3V4FeatureTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1, 1, 0, 0, 1];
    private static readonly byte[] Eicar = Encoding.ASCII.GetBytes("X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
    private static string Key() => Guid.NewGuid().ToString("N");
    private static UpdateProfileRequest Req(string name, string headline = "Founder & CEO") => new(name, headline, "Dhaka", "Bio", null);

    private static Task<int> PitchId(TestHost h, string seedKey) => h.Db(db => db.ConnectionRequests.Where(r => r.IdempotencyKey == seedKey).Select(r => r.Id).SingleAsync());

    // ======================================================================= v4 · profiles
    [Fact]
    public async Task P01_VerifiedFounder_NameChange_GoesToReview_AndOldNameStaysLive()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var r = await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(f, Req("Farhan Kabir Chowdhury", "CEO, Nodi Freight")));
        Assert.True(r.IsSuccess, r.Error.Message);
        Assert.Equal("Farhan Kabir", await h.Db(db => db.UserAccounts.Where(u => u.Id == f.AccountId).Select(u => u.FullName).SingleAsync()));
        var me = await h.Run<IProfileFacade, Result<MyProfileDto>>(x => x.GetMineAsync(f));
        Assert.Equal("Farhan Kabir Chowdhury", me.Value!.PendingName);
        Assert.Equal("CEO, Nodi Freight", me.Value.Headline);          // non-name fields apply immediately
        Assert.False(me.Value.CanEditName);
        var again = await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(f, Req("Farhan K. Chowdhury")));
        Assert.Equal(ErrorCodes.NameChangePending, again.Error.Code);
    }

    [Fact]
    public async Task P02_Approval_RenamesEverywhere_AndStartsThe30DayCooldown()
    {
        await using var h = await TestHost.CreateAsync();
        var sadia = await h.Investor();
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia Haque Rahman", "Partner")))).IsSuccess);
        var admin = await h.Admin("verify");
        var list = await h.Run<IProfileFacade, IReadOnlyList<NameChangeDto>>(x => x.ListNameChangesAsync(admin));
        var id = list.Single(x => x.Status == NameChangeStatus.Pending).Id;
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.DecideNameChangeAsync(admin, id, true, null))).IsSuccess);
        Assert.Equal("Sadia Haque Rahman", await h.Db(db => db.InvestorProfiles.Where(i => i.Id == sadia.InvestorId).Select(i => i.DisplayName).SingleAsync()));
        Assert.Equal("Sadia Haque Rahman", await h.Db(db => db.UserAccounts.Where(u => u.Id == sadia.AccountId).Select(u => u.FullName).SingleAsync()));
        Assert.All(await h.Db(db => db.PostComments.Where(c => c.AuthorAccountId == sadia.AccountId).ToListAsync()), c => Assert.Equal("Sadia Haque Rahman", c.AuthorName));
        Assert.All(await h.Db(db => db.Messages.Where(m => m.AuthorAccountId == sadia.AccountId).ToListAsync()), m => Assert.Equal("Sadia Haque Rahman", m.AuthorName));
        Assert.Contains(await h.Db(db => db.AdminAudit.Select(a => a.Action).ToListAsync()), a => a == "Name change approved");
        // 30-day cooldown, then allowed again.
        var tooSoon = await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia Rahman", "Partner")));
        Assert.Equal(ErrorCodes.NameChangeCooldown, tooSoon.Error.Code);
        h.Clock.Advance(TimeSpan.FromDays(31));
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia Rahman", "Partner")))).IsSuccess);
    }

    [Fact]
    public async Task P03_Rejection_NeedsAReason_KeepsTheName_AndNotifies()
    {
        await using var h = await TestHost.CreateAsync();
        var sadia = await h.Investor();
        await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia H. Rahman", "Partner")));
        var admin = await h.Admin("verify");
        var id = (await h.Run<IProfileFacade, IReadOnlyList<NameChangeDto>>(x => x.ListNameChangesAsync(admin))).Single().Id;
        Assert.Equal(ErrorCodes.Validation, (await h.Run<IProfileFacade, Result>(x => x.DecideNameChangeAsync(admin, id, false, ""))).Error.Code);
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.DecideNameChangeAsync(admin, id, false, "Does not match NID on file"))).IsSuccess);
        Assert.Equal("Sadia Haque", await h.Db(db => db.InvestorProfiles.Where(i => i.Id == sadia.InvestorId).Select(i => i.DisplayName).SingleAsync()));
        Assert.True(await h.Db(db => db.Notifications.AnyAsync(n => n.RecipientAccountId == sadia.AccountId && n.Text.Contains("not approved"))));
    }

    [Theory]
    [InlineData("Admin Farhan"), InlineData("RiseUp Official"), InlineData("Farhan 123"), InlineData("F"), InlineData("<script>")]
    public async Task P04_InvalidOrImpersonatingNames_AreRejected(string name)
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var r = await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(f, Req(name)));
        Assert.Equal(ErrorCodes.Validation, r.Error.Code);
        Assert.Equal(0, await h.Db(db => db.NameChangeRequests.CountAsync()));
    }

    [Fact]
    public async Task P05_UnverifiedFounder_ChangesNameImmediately_AndNewCommentsUseIt()
    {
        await using var h = await TestHost.CreateAsync();
        var ayesha = await h.PendingFounder();
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(ayesha, Req("Ayesha Noor Rahman")))).IsSuccess);
        Assert.Equal("Ayesha Noor Rahman", await h.Db(db => db.UserAccounts.Where(u => u.Id == ayesha.AccountId).Select(u => u.FullName).SingleAsync()));
        // A verified investor renamed by an admin must not keep commenting under the stale sign-in name.
        var sadia = await h.Investor();
        await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia Haque Rahman", "Partner")));
        var admin = await h.Admin("verify");
        var id = (await h.Run<IProfileFacade, IReadOnlyList<NameChangeDto>>(x => x.ListNameChangesAsync(admin))).Single().Id;
        await h.Run<IProfileFacade, Result>(x => x.DecideNameChangeAsync(admin, id, true, null));
        var postId = await h.Db(db => db.Posts.Select(p => p.Id).FirstAsync());
        var c = await h.Run<IPostFacade, Result<int>>(x => x.CommentAsync(sadia /* stale cookie name "Sadia Haque" */, postId, null, "Congratulations!"));
        Assert.Equal("Sadia Haque Rahman", await h.Db(db => db.PostComments.Where(x => x.Id == c.Value).Select(x => x.AuthorName).SingleAsync()));
    }

    [Fact]
    public async Task P06_TeamMember_CanChangeProfileImages()
    {
        await using var h = await TestHost.CreateAsync();
        var rafi = await h.Team();
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(rafi, Req("Rafi Ahmed Khan")))).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IProfileFacade, Result>(async x => await x.SetCoverAsync(rafi, await h.UploadAsync(Jpeg, "c.jpg", "image/jpeg")))).Error.Code);
        var photo = await h.UploadAsync(Jpeg, "me.jpg", "image/jpeg");
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.SetPhotoAsync(rafi, photo))).IsSuccess);
        var shell = await h.Run<IProfileFacade, ShellDto>(x => x.GetShellAsync(rafi));
        Assert.StartsWith("/media/", shell.PhotoUrl);
        var owner = await h.Founder();
        var companyLogo = await h.Db(db => db.Startups.Where(s => s.Id == owner.StartupId).Select(s => s.LogoMediaId).SingleAsync());
        var logo = await h.UploadAsync(Jpeg, "personal.jpg", "image/jpeg");
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.SetLogoAsync(rafi, logo))).IsSuccess);
        var member = (await h.Run<IProfileFacade, Result<MyProfileDto>>(x => x.GetMineAsync(rafi))).Value!;
        Assert.StartsWith("/media/", member.LogoUrl);
        Assert.Equal(shell.PhotoUrl, member.PhotoUrl);
        Assert.Null((await h.Run<IProfileFacade, Result<MyProfileDto>>(x => x.GetMineAsync(owner))).Value!.LogoUrl);
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.RemoveLogoAsync(rafi))).IsSuccess);
        var after = (await h.Run<IProfileFacade, Result<MyProfileDto>>(x => x.GetMineAsync(rafi))).Value!;
        Assert.Null(after.LogoUrl);
        Assert.Equal(shell.PhotoUrl, after.PhotoUrl);
        Assert.Equal(companyLogo, await h.Db(db => db.Startups.Where(s => s.Id == owner.StartupId).Select(s => s.LogoMediaId).SingleAsync()));
    }

    [Fact]
    public async Task GalleryUpgradeFillsMissingPortraitAndPreservesOwnerEditsOnRepeat()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var investor = await h.Investor();
        var mediaCount = await h.Db(db => db.MediaAssets.CountAsync());
        var postCount = await h.Db(db => db.Posts.CountAsync());
        // Simulate an older demo in which the investor portrait has never been installed.
        await h.Db(async db =>
        {
            var profile = await db.MemberProfiles.SingleAsync(p => p.AccountId == investor.AccountId);
            var photo = await db.MediaAssets.SingleAsync(m => m.Id == profile.PhotoMediaId);
            profile.PhotoMediaId = null;
            db.MediaAssets.Remove(photo);
            var reel = await db.Posts.SingleAsync(p => p.StartupId == f.StartupId && p.Category == PostCategory.Reel);
            reel.LiveTitle = "Edited by the founder";
            return await db.SaveChangesAsync();
        });
        Assert.True((await h.Run<IProfileFacade, Result>(x => x.RemovePhotoAsync(f))).IsSuccess);
        for (var i = 0; i < 2; i++)
            await h.Run<RiseUp.DataAccess.Seed.DemoDataSeeder, bool>(async seed => { await seed.SeedAsync(); return true; });
        Assert.NotNull(await h.Db(db => db.MemberProfiles.Where(p => p.AccountId == investor.AccountId).Select(p => p.PhotoMediaId).SingleAsync()));
        Assert.Null(await h.Db(db => db.MemberProfiles.Where(p => p.AccountId == f.AccountId).Select(p => p.PhotoMediaId).SingleAsync()));
        Assert.Equal(mediaCount, await h.Db(db => db.MediaAssets.CountAsync()));
        Assert.Equal(postCount, await h.Db(db => db.Posts.CountAsync()));
        Assert.True(await h.Db(db => db.Posts.AnyAsync(p => p.LiveTitle == "Edited by the founder")));
    }

    [Fact]
    public async Task P07_PhotoUploads_AreTypeSizeAndMalwareChecked()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var originalPhotos = await h.Db(db => db.MediaAssets.CountAsync(m => m.Kind == MediaKind.ProfilePhoto));
        Assert.Equal(ErrorCodes.InvalidMedia, (await h.Run<IProfileFacade, Result>(async x => await x.SetPhotoAsync(f, await h.UploadAsync(Jpeg, "x.pdf", "application/pdf")))).Error.Code);
        var big = await h.UploadAsync(Jpeg, "big.jpg", "image/jpeg") with { SizeBytes = 6 * 1024 * 1024 };
        Assert.Equal(ErrorCodes.InvalidMedia, (await h.Run<IProfileFacade, Result>(x => x.SetPhotoAsync(f, big))).Error.Code);
        Assert.Equal(ErrorCodes.MediaInfected, (await h.Run<IProfileFacade, Result>(async x => await x.SetPhotoAsync(f, await h.UploadAsync(Eicar, "eicar.jpg", "image/jpeg")))).Error.Code);
        Assert.Equal(originalPhotos, await h.Db(db => db.MediaAssets.CountAsync(m => m.Kind == MediaKind.ProfilePhoto)));
    }

    [Fact]
    public async Task P08_OnlyVerificationAdmins_ReviewNames_AndOnlyOwnersBrandTheStartup()
    {
        await using var h = await TestHost.CreateAsync();
        var sadia = await h.Investor();
        await h.Run<IProfileFacade, Result>(x => x.UpdateAsync(sadia, Req("Sadia Haque Rahman", "Partner")));
        var mod = await h.Admin("moderator");
        Assert.Empty(await h.Run<IProfileFacade, IReadOnlyList<NameChangeDto>>(x => x.ListNameChangesAsync(mod)));
        var id = await h.Db(db => db.NameChangeRequests.Select(r => r.Id).SingleAsync());
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IProfileFacade, Result>(x => x.DecideNameChangeAsync(mod, id, true, null))).Error.Code);
        var logo = await h.UploadAsync(Jpeg, "logo.jpg", "image/jpeg");
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IProfileFacade, Result>(x => x.SetStartupImageAsync(sadia, MediaKind.StartupLogo, logo))).Error.Code);
        var f = await h.Founder();
        Assert.True((await h.Run<IProfileFacade, Result>(async x => await x.SetStartupImageAsync(f, MediaKind.StartupLogo, await h.UploadAsync(Jpeg, "l.jpg", "image/jpeg")))).IsSuccess);
        Assert.NotNull(await h.Db(db => db.Startups.Where(s => s.Id == f.StartupId).Select(s => s.LogoMediaId).SingleAsync()));
    }

    // ======================================================================= v3 · pitch package
    [Fact]
    public async Task K01_AskMustTotal100Percent_AndEveryChangeBumpsTheVersion()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var v0 = (await h.Run<IPitchPackageFacade, Result<PitchPackageDto>>(x => x.GetOwnAsync(f))).Value!.Version;
        var bad = await h.Run<IPitchPackageFacade, Result>(x => x.SaveAskAsync(f, new AskInput(600000, FundingInstrument.Safe, 4500000, 20, 18, 25000, [new("Hiring", 60), new("Ops", 30)])));
        Assert.Equal(ErrorCodes.Validation, bad.Error.Code);
        Assert.Contains("100%", bad.Error.Message);
        Assert.True((await h.Run<IPitchPackageFacade, Result>(x => x.SaveAskAsync(f, new AskInput(700000, FundingInstrument.Equity, 5000000, 0, 18, 25000, [new("Hiring", 60), new("Ops", 40)])))).IsSuccess);
        var dto = (await h.Run<IPitchPackageFacade, Result<PitchPackageDto>>(x => x.GetOwnAsync(f))).Value!;
        Assert.Equal(v0 + 1, dto.Version);
        Assert.Equal(700000, dto.Ask.AmountUsd);
        var investor = await h.Investor();
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchPackageFacade, Result>(x => x.SaveAskAsync(investor, new AskInput(1, FundingInstrument.Safe, 0, 0, 1, 0, [new("x", 100)])))).Error.Code);
    }

    [Fact]
    public async Task K02_VideoLengthLimits_AreEnforced()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var tooLong = await h.UploadAsync([0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p'], "teaser.mp4", "video/mp4", 61);
        Assert.Equal(ErrorCodes.InvalidMedia, (await h.Run<IPitchPackageFacade, Result>(x => x.SetVideoAsync(f, PitchVideoSlot.Teaser, tooLong, null))).Error.Code);
        var unknown = await h.UploadAsync([1, 2, 3], "pitch.mp4", "video/mp4", null);
        Assert.Equal(ErrorCodes.InvalidMedia, (await h.Run<IPitchPackageFacade, Result>(x => x.SetVideoAsync(f, PitchVideoSlot.Pitch, unknown, null))).Error.Code);
        var ok = await h.UploadAsync([0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p'], "pitch.mp4", "video/mp4", 170);
        Assert.True((await h.Run<IPitchPackageFacade, Result>(x => x.SetVideoAsync(f, PitchVideoSlot.Pitch, ok, null))).IsSuccess);
    }

    [Fact]
    public async Task K03_AtMostSixPitchDocuments()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder();
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF");
        for (var i = 0; i < 4; i++)   // the seed already holds 2
            Assert.True((await h.Run<IPitchPackageFacade, Result>(async x => await x.AddDocumentAsync(f, await h.UploadAsync(pdf, $"d{i}.pdf", "application/pdf"), "Doc"))).IsSuccess);
        var seventh = await h.Run<IPitchPackageFacade, Result>(async x => await x.AddDocumentAsync(f, await h.UploadAsync(pdf, "d7.pdf", "application/pdf"), "Doc"));
        Assert.Equal(ErrorCodes.LimitReached, seventh.Error.Code);
    }

    [Fact]
    public async Task K04_SendingAPitch_SnapshotsVersionAndChosenDocs_AndOnlyTheNoteReachesAI()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder(); var imran = await h.Imran();
        var docIds = await h.Db(db => db.PitchDocuments.Where(d => db.PitchPackages.Any(p => p.Id == d.PackageId && p.StartupId == f.StartupId)).Select(d => d.Id).ToListAsync());
        var callsBefore = h.Model1.Calls;
        var r = await h.Run<IConnectionRequestFacade, Result<int>>(x => x.SendPitchAsync(f, new(imran.InvestorId!.Value, "We would value your agri supply-chain experience.", Key(), null, [docIds[0]])));
        Assert.True(r.IsSuccess, r.Error.Message);
        var req = await h.Db(db => db.ConnectionRequests.SingleAsync(x => x.Id == r.Value));
        Assert.Equal(docIds[0].ToString(), req.AttachedDocumentIds);
        Assert.Equal(await h.Db(db => db.PitchPackages.Where(p => p.StartupId == f.StartupId).Select(p => p.PackageVersion).SingleAsync()), req.PackageVersion);
        Assert.Equal(callsBefore + 1, h.Model1.Calls);
        Assert.Equal("pitch", h.Model1.Purposes[^1]);
    }

    // ======================================================================= v3 · pitch room
    [Fact]
    public async Task R01_PitchRoom_IsOnlyForTheReceivingInvestorAndTheFounder()
    {
        await using var h = await TestHost.CreateAsync();
        var takaPitch = await PitchId(h, "seed:r4");
        var sadia = await h.Investor(); var imran = await h.Imran(); var nodi = await h.Founder();
        var taka = await h.SignIn("hello@takaledger.example");
        Assert.True((await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(sadia, takaPitch))).IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(imran, takaPitch))).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(nodi, takaPitch))).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(Actor.Anonymous, takaPitch))).Error.Code);
        var founderView = await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(taka, takaPitch));
        Assert.NotNull(founderView.Value!.Stats);           // analytics only for the founder
        Assert.Null((await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(sadia, takaPitch))).Value!.Stats);
        Assert.True((await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(nodi, 0))).Value!.IsPreview);
    }

    [Fact]
    public async Task R02_FirstOpenByInvestor_IsRecordedAndNotified_FounderViewsAreNotTracked()
    {
        await using var h = await TestHost.CreateAsync();
        var jutePitch = await PitchId(h, "seed:r5");
        var jute = await h.SignIn("tasnim@juteloop.example"); var sadia = await h.Investor();
        await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(jute, jutePitch));
        Assert.Null(await h.Db(db => db.PitchEngagements.Where(e => e.RequestId == jutePitch).Select(e => e.OpenedAtUtc).SingleOrDefaultAsync()));
        await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(sadia, jutePitch));
        Assert.NotNull(await h.Db(db => db.PitchEngagements.Where(e => e.RequestId == jutePitch).Select(e => e.OpenedAtUtc).SingleAsync()));
        Assert.True(await h.Db(db => db.Notifications.AnyAsync(n => n.RecipientAccountId == jute.AccountId && n.Text.Contains("opened your pitch room"))));
        await h.Run<IPitchRoomFacade, Result>(x => x.TrackAsync(sadia, jutePitch, new TrackInput("video", PitchVideoSlot.Pitch, 55, null, null)));
        await h.Run<IPitchRoomFacade, Result>(x => x.TrackAsync(sadia, jutePitch, new TrackInput("video", PitchVideoSlot.Pitch, 30, null, null)));   // never goes backwards
        await h.Run<IPitchRoomFacade, Result>(x => x.TrackAsync(jute, jutePitch, new TrackInput("video", PitchVideoSlot.Pitch, 100, null, null)));   // ignored
        await h.Run<IPitchRoomFacade, Result>(x => x.TrackAsync(sadia, jutePitch, new TrackInput("doc", null, null, null, 999999)));                // not attached: ignored
        var e = await h.Db(db => db.PitchEngagements.SingleAsync(x => x.RequestId == jutePitch));
        Assert.Equal(55, e.PitchWatchPercent);
        Assert.Equal("", e.DocumentsViewed);
    }

    [Fact]
    public async Task R03_QandA_IsCappedAtFive_OwnerAnswers_AndNothingGoesToAI()
    {
        await using var h = await TestHost.CreateAsync();
        var takaPitch = await PitchId(h, "seed:r4");   // already has 1 question
        var sadia = await h.Investor(); var taka = await h.SignIn("hello@takaledger.example");
        var calls = h.Model1.Calls;
        for (var i = 0; i < 4; i++) Assert.True((await h.Run<IPitchRoomFacade, Result>(x => x.AskAsync(sadia, takaPitch, $"Question {i}?"))).IsSuccess);
        Assert.Equal(ErrorCodes.LimitReached, (await h.Run<IPitchRoomFacade, Result>(x => x.AskAsync(sadia, takaPitch, "Sixth?"))).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchRoomFacade, Result>(x => x.AskAsync(taka, takaPitch, "Self?"))).Error.Code);
        var qid = await h.Db(db => db.PitchQuestions.Where(q => q.RequestId == takaPitch && q.Answer == null).Select(q => q.Id).FirstAsync());
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPitchRoomFacade, Result>(x => x.AnswerAsync(sadia, qid, "I answer myself"))).Error.Code);
        Assert.True((await h.Run<IPitchRoomFacade, Result>(x => x.AnswerAsync(taka, qid, "Here is the answer."))).IsSuccess);
        Assert.Equal(calls, h.Model1.Calls);
    }

    [Fact]
    public async Task R04_RequestMoreInfo_DoesNotPauseTheDecisionWindow()
    {
        await using var h = await TestHost.CreateAsync();
        var takaPitch = await PitchId(h, "seed:r4");
        var sadia = await h.Investor();
        var before = await h.Db(db => db.ConnectionRequests.Where(r => r.Id == takaPitch).Select(r => r.ExpiresAtUtc).SingleAsync());
        Assert.True((await h.Run<IPitchRoomFacade, Result>(x => x.RequestMoreInfoAsync(sadia, takaPitch, "Please share lender concentration."))).IsSuccess);
        var after = await h.Db(db => db.ConnectionRequests.SingleAsync(r => r.Id == takaPitch));
        Assert.Equal(before, after.ExpiresAtUtc);
        Assert.Equal(RequestStatus.Pending, after.Status);
        Assert.NotNull(after.InfoRequestedAtUtc);
    }

    [Fact]
    public async Task InformationRequests_PreserveEachItemAndScopeRepliesToThePitch()
    {
        await using var h = await TestHost.CreateAsync();
        var pitch = await PitchId(h, "seed:r4");
        var investor = await h.Investor();
        var founder = await h.SignIn("hello@takaledger.example");
        var outsider = await h.Founder();
        var otherInvestor = await h.Imran();

        Task<Result> Ask(string prompts) => h.Run<IPitchRoomFacade, Result>(x => x.RequestMoreInfoAsync(investor, pitch, prompts));
        Task<Result> Reply(Actor actor, int item, string? text, UploadedFile? file = null) =>
            h.Run<IPitchRoomFacade, Result>(x => x.ReplyToInfoAsync(actor, pitch, item, text, file));
        Assert.True((await Ask("Revenue breakdown\r\n\nFinancial model")).IsSuccess);
        Assert.True((await Ask("Cash runway")).IsSuccess);
        var items = (await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(founder, pitch))).Value!.InfoItems;
        Assert.Equal(new[] { "Revenue breakdown", "Financial model", "Cash runway" }, items.Select(i => i.Prompt));
        Assert.Equal(ErrorCodes.Forbidden, (await Reply(outsider, items[0].Id, "Wrong startup")).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await Reply(investor, items[0].Id, "Self reply")).Error.Code);
        Assert.Equal(ErrorCodes.Validation, (await Reply(founder, items[0].Id, " ")).Error.Code);
        Assert.True((await Reply(founder, items[0].Id, "Quarterly revenue grew 12%.")).IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, (await Reply(founder, items[0].Id, "Duplicate")).Error.Code);
        var rejected = await h.UploadAsync(Eicar, "rejected.pdf", "application/pdf");
        Assert.Equal(ErrorCodes.MediaInfected, (await Reply(founder, items[1].Id, null, rejected)).Error.Code);
        var file = await h.UploadAsync("%PDF-1.4 sample"u8.ToArray(), "forecast.pdf", "application/pdf");
        Assert.True((await Reply(founder, items[1].Id, null, file)).IsSuccess);
        var replies = (await h.Run<IPitchRoomFacade, Result<PitchRoomDto>>(x => x.GetAsync(investor, pitch))).Value!.InfoItems;
        Assert.Equal("Quarterly revenue grew 12%.", replies[0].ReplyText);
        Assert.Equal("forecast.pdf", replies[1].FileName);
        Assert.Null(replies[2].AnsweredAtUtc);
        var mediaId = await h.Db(db => db.PitchInfoItems.Where(i => i.Id == items[1].Id).Select(i => i.ReplyMediaId!.Value).SingleAsync());
        foreach (var actor in new[] { Actor.Anonymous, outsider, otherInvestor })
            Assert.Equal(ErrorCodes.NotFound, (await h.Run<IMediaFacade, Result<MediaStream>>(x => x.OpenAsync(actor, mediaId, true))).Error.Code);
        var download = await h.Run<IMediaFacade, Result<MediaStream>>(x => x.OpenAsync(investor, mediaId, true));
        Assert.Equal("forecast.pdf", download.Value!.FileName);
        await download.Value.Content.DisposeAsync();
        h.Clock.Advance(TimeSpan.FromDays(15));
        Assert.Equal(ErrorCodes.NotPending, (await Reply(founder, items[2].Id, "Late reply")).Error.Code);
    }

    [Fact]
    public async Task LegacyInformationRequest_IsImportedOnceAndCanBeAnswered()
    {
        await using var h = await TestHost.CreateAsync();
        var pitch = await PitchId(h, "seed:r4");
        var item = await h.Db(async db =>
        {
            var request = await db.ConnectionRequests.FindAsync(pitch);
            request!.InfoRequestMessage = "Please share the forecast.";
            request.InfoRequestedAtUtc = h.Clock.UtcNow;
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE PitchInfoItems");
            await db.InitializeDemoDatabaseAsync();
            await db.InitializeDemoDatabaseAsync();
            return await db.PitchInfoItems.SingleAsync(i => i.RequestId == pitch);
        });
        Assert.Equal("Please share the forecast.", item.Prompt);
        var founder = await h.SignIn("hello@takaledger.example");
        var reply = await h.Run<IPitchRoomFacade, Result>(x => x.ReplyToInfoAsync(founder, pitch, item.Id, "Here is our forecast.", null));
        Assert.True(reply.IsSuccess, reply.Error.Message);
    }

    // ======================================================================= media access
    [Fact]
    public async Task M01_PrivatePitchMedia_OnlyForOwnerAndRecipients_TeasersPublicWhenOptedIn()
    {
        await using var h = await TestHost.CreateAsync();
        var taka = await h.SignIn("hello@takaledger.example");
        var pkg = await h.Db(db => db.PitchPackages.Include(p => p.Documents).SingleAsync(p => p.StartupId == taka.StartupId));
        Assert.NotNull(pkg.PitchVideoId);   // demo videos imported by the seeder
        var sadia = await h.Investor(); var imran = await h.Imran();
        async Task<string> Open(Actor a, int id, bool download = false)
        {
            var r = await h.Run<IMediaFacade, Result<MediaStream>>(x => x.OpenAsync(a, id, download));
            if (r.IsSuccess) await r.Value!.Content.DisposeAsync();
            return r.IsSuccess ? "ok" : r.Error.Code;
        }
        Assert.Equal(ErrorCodes.NotFound, await Open(Actor.Anonymous, pkg.PitchVideoId!.Value));
        Assert.Equal(ErrorCodes.NotFound, await Open(imran, pkg.PitchVideoId.Value));       // no pitch from Taka Ledger
        Assert.Equal("ok", await Open(sadia, pkg.PitchVideoId.Value));                      // pending pitch recipient
        Assert.Equal("ok", await Open(taka, pkg.PitchVideoId.Value));
        Assert.Equal("ok", await Open(Actor.Anonymous, pkg.TeaserVideoId!.Value));          // public teaser
        await h.Run<IPitchPackageFacade, Result>(x => x.SetVisibilityAsync(taka, false, false, true));
        Assert.Equal(ErrorCodes.NotFound, await Open(Actor.Anonymous, pkg.TeaserVideoId.Value));
        var viewOnly = pkg.Documents.Single(d => !d.AllowDownload);
        Assert.Equal("ok", await Open(sadia, viewOnly.MediaId));
        Assert.Equal(ErrorCodes.ForbiddenDocument, await Open(sadia, viewOnly.MediaId, download: true));
        Assert.Equal(ErrorCodes.NotFound, await Open(imran, viewOnly.MediaId));
    }

    [Fact]
    public async Task M02_Spotlight_ShowsTheAskOnlyToVerifiedInvestors()
    {
        await using var h = await TestHost.CreateAsync();
        var visitor = await h.Run<IPitchPackageFacade, IReadOnlyList<SpotlightDto>>(x => x.ListSpotlightAsync(Actor.Anonymous, 10));
        Assert.NotEmpty(visitor);
        Assert.All(visitor, s => { Assert.Null(s.Ask); Assert.Null(s.Metrics); });
        var sadia = await h.Investor();
        var investor = await h.Run<IPitchPackageFacade, IReadOnlyList<SpotlightDto>>(x => x.ListSpotlightAsync(sadia, 10));
        Assert.All(investor, s => Assert.NotNull(s.Ask));
        Assert.Contains(investor, s => s.PendingPitchRequestId is not null);
        var founder = await h.Founder();
        Assert.DoesNotContain(await h.Run<IPitchPackageFacade, IReadOnlyList<SpotlightDto>>(x => x.ListSpotlightAsync(founder, 10)), s => s.StartupId == founder.StartupId);
    }

    // ======================================================================= shortlist, meetings, saved
    [Fact]
    public async Task S01_Shortlist_IsInvestorOnly_AndStartsAtTheRightStage()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder(); var sadia = await h.Investor(); var imran = await h.Imran();
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IShortlistFacade, Result<bool>>(x => x.ToggleAsync(f, 2))).Error.Code);
        var jute = await h.Db(db => db.Startups.Where(s => s.Name == "Jute Loop").Select(s => s.Id).SingleAsync());
        Assert.True((await h.Run<IShortlistFacade, Result<bool>>(x => x.ToggleAsync(sadia, jute))).Value);
        Assert.Equal(ShortlistStage.PitchReceived, await h.Db(db => db.Shortlist.Where(s => s.InvestorId == sadia.InvestorId && s.StartupId == jute).Select(s => s.Stage).SingleAsync()));
        Assert.False((await h.Run<IShortlistFacade, Result<bool>>(x => x.ToggleAsync(sadia, jute))).Value);
        Assert.Empty(await h.Run<IShortlistFacade, IReadOnlyList<ShortlistItemDto>>(x => x.ListAsync(imran)));   // private per investor
    }

    [Fact]
    public async Task S02_Meetings_OnlyBetweenConnectedParties_TheOtherSideConfirms()
    {
        await using var h = await TestHost.CreateAsync();
        var f = await h.Founder(); var sadia = await h.Investor(); var rafi = await h.Team(); var imran = await h.Imran();
        var conv = await h.Db(db => db.Conversations.Where(c => c.StartupId == f.StartupId && c.InvestorId == sadia.InvestorId).Select(c => c.Id).SingleAsync());
        var past = await h.Run<IMeetingFacade, Result<int>>(x => x.ProposeAsync(f, conv, [h.Clock.UtcNow.AddHours(-1)], null));
        Assert.Equal(ErrorCodes.Validation, past.Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IMeetingFacade, Result<int>>(x => x.ProposeAsync(rafi, conv, [h.Clock.UtcNow.AddDays(2)], null))).Error.Code);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IMeetingFacade, Result<int>>(x => x.ProposeAsync(imran, conv, [h.Clock.UtcNow.AddDays(2)], null))).Error.Code);
        var slot = new DateTime(2026, 11, 10, 5, 0, 0, DateTimeKind.Utc);
        var m = await h.Run<IMeetingFacade, Result<int>>(x => x.ProposeAsync(f, conv, [slot, slot.AddDays(1)], "Corridor economics"));
        Assert.True(m.IsSuccess, m.Error.Message);
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IMeetingFacade, Result>(x => x.AcceptAsync(f, m.Value, slot))).Error.Code);
        Assert.Equal(ErrorCodes.NotFound, (await h.Run<IMeetingFacade, Result<(DateTime, string, string)>>(x => x.GetCalendarAsync(sadia, m.Value))).Error.Code);
        Assert.True((await h.Run<IMeetingFacade, Result>(x => x.AcceptAsync(sadia, m.Value, slot))).IsSuccess);
        Assert.True((await h.Run<IMeetingFacade, Result<(DateTime, string, string)>>(x => x.GetCalendarAsync(rafi, m.Value))).IsSuccess);   // team can see it
        Assert.True(await h.Db(db => db.Messages.AnyAsync(x => x.ConversationId == conv && x.IsSystem && x.Body.Contains("Meeting confirmed"))));
    }

    [Fact]
    public async Task S03_SavedPosts_ArePrivateBookmarks()
    {
        await using var h = await TestHost.CreateAsync();
        var sadia = await h.Investor(); var imran = await h.Imran();
        var postId = await h.Db(db => db.Posts.Select(p => p.Id).FirstAsync());
        Assert.True((await h.Run<IPostFacade, Result<bool>>(x => x.ToggleSaveAsync(sadia, postId))).Value);
        Assert.Single(await h.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(x => x.GetFeedAsync(sadia, "saved", null, 20)));
        Assert.Empty(await h.Run<IPostFacade, IReadOnlyList<FeedItemDto>>(x => x.GetFeedAsync(imran, "saved", null, 20)));
        Assert.Equal(ErrorCodes.Forbidden, (await h.Run<IPostFacade, Result<bool>>(x => x.ToggleSaveAsync(Actor.Anonymous, postId))).Error.Code);
    }
}
