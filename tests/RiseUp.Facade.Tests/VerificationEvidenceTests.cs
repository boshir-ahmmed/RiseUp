using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Verification;
using RiseUp.Facade.Common;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Identity;
using RiseUp.Facade.Verification;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Tests;

public class VerificationEvidenceTests
{
    [Fact]
    public async Task ApplicantSeesOwnSubmittedDocumentKinds()
    {
        await using var h = await TestHost.CreateAsync();
        var owner = await h.PendingFounder();
        var status = await h.Run<IVerificationFacade, Result<ApplicantStatusDto>>(v => v.GetApplicantStatusAsync(owner));
        Assert.True(status.Value!.AwaitingReview);
        Assert.Contains("TradeLicence", status.Value.DocumentKinds);
        var other = await h.Founder();
        var wrongAccount = owner with { AccountId = other.AccountId };
        var denied = await h.Run<IVerificationFacade, Result<ApplicantStatusDto>>(v => v.GetApplicantStatusAsync(wrongAccount));
        Assert.True(denied.IsFailure);
    }

    [Theory]
    [InlineData("stream: OK", ScanVerdict.Clean)]
    [InlineData("stream: sample FOUND", ScanVerdict.Infected)]
    [InlineData("INSTREAM size limit exceeded. ERROR", ScanVerdict.Unavailable)]
    public async Task ClamAvStreamsTheExactFileAndRequiresAnExplicitCleanReply(string reply, ScanVerdict expected)
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var bytes = new byte[70000];
        new Random(1).NextBytes(bytes);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var scan = new RiseUp.DataAccess.Services.ClamAvScanner("127.0.0.1", port).ScanAsync(new MemoryStream(bytes), timeout.Token);
        using var client = await listener.AcceptTcpClientAsync(timeout.Token);
        var stream = client.GetStream();
        var command = new byte[10];
        await stream.ReadExactlyAsync(command, timeout.Token);
        Assert.Equal("zINSTREAM\0"u8.ToArray(), command);
        using var received = new MemoryStream();
        var size = new byte[4];
        while (true)
        {
            await stream.ReadExactlyAsync(size, timeout.Token);
            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(size);
            if (length == 0) break;
            Assert.InRange(length, 1, 65536);
            var chunk = new byte[length];
            await stream.ReadExactlyAsync(chunk, timeout.Token);
            received.Write(chunk);
        }
        Assert.Equal(bytes, received.ToArray());
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(reply + "\0"), timeout.Token);
        Assert.Equal(expected, await scan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeletionErasesOnlyTheDeletedSubjectAcrossAllApplicationsAndHonoursHolds(bool deleteFounder)
    {
        await using var h = await TestHost.CreateAsync();
        var actor = deleteFounder ? await h.PendingFounder() : await h.Imran();
        var admin = await h.Admin("super");
        var bytes = "%PDF fictional evidence"u8.ToArray();
        var first = await h.UploadAsync(bytes, "first.pdf", "application/pdf");
        var second = await h.UploadAsync(bytes, "second.pdf", "application/pdf");
        var other = await h.UploadAsync(bytes, "other.pdf", "application/pdf");
        var otherActor = deleteFounder ? await h.Founder() : await h.Investor();

        VerificationApplication Application(Actor owner, string key) => new()
        {
            Subject = owner.IsFounder ? VerificationSubject.Startup : VerificationSubject.Investor,
            StartupId = owner.StartupId, InvestorId = owner.InvestorId, Status = VerificationStatus.Rejected,
            Documents = [new VerificationDocument { Kind = "NID", StorageKey = key }]
        };
        var old = Application(actor, first.StorageKey);
        var held = Application(actor, second.StorageKey);
        var preserved = Application(otherActor, other.StorageKey);
        await h.Db(async db =>
        {
            db.AddRange(old, held, preserved);
            for (var i = 0; i < 205; i++)
                db.VerificationApplications.Add(new() { Subject = preserved.Subject, StartupId = preserved.StartupId,
                    InvestorId = preserved.InvestorId, Status = VerificationStatus.Verified, SubmittedAtUtc = h.Clock.UtcNow });
            return await db.SaveChangesAsync();
        });

        var hold = await h.Run<IVerificationFacade, Result>(v => v.SetEvidenceHoldAsync(admin, held.Id, held.Documents[0].Id, true, "Case under review"));
        Assert.True(hold.IsSuccess);
        Assert.True((await h.Run<IAccountFacade, Result>(a => a.DeleteAccountAsync(actor, "DELETE"))).IsSuccess);
        async Task Purge() => await h.Run<IVerificationFacade, int>(async v => { await v.ProcessEvidenceErasureAsync(); return 0; });
        async Task<bool> Exists(string key)
        {
            await using var stream = await h.Services.GetRequiredService<IFileStorage>().OpenAsync(key);
            return stream != null;
        }
        await Purge();
        Assert.False(await Exists(first.StorageKey));
        Assert.True(await Exists(second.StorageKey));
        Assert.True(await Exists(other.StorageKey));
        Assert.True(await h.Db(db => db.VerificationDocuments.AnyAsync(d => d.Id == old.Documents[0].Id && d.Erased)));
        Assert.False(await h.Db(db => db.VerificationDocuments.AnyAsync(d => d.Id == preserved.Documents[0].Id && d.Erased)));
        Assert.True((await h.Run<IVerificationFacade, Result>(v => v.SetEvidenceHoldAsync(admin, held.Id, held.Documents[0].Id, false, "Case closed"))).IsSuccess);
        await Purge();
        await Purge();
        Assert.False(await Exists(second.StorageKey));
        Assert.Equal(1, await h.Db(db => db.AdminAudit.CountAsync(a => a.Action == "Verification evidence erased" && a.Target == $"verification-document:{held.Documents[0].Id}")));
    }

    [Fact]
    public async Task InterruptedUploadsAndFailedErasureAreRetriedWithoutLosingReferencedFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"riseup-evidence-{Guid.NewGuid():N}");
        try
        {
            await using var h = await TestHost.CreateAsync(new() { ["Storage:RootPath"] = root });
            var intake = h.Services.GetRequiredService<IVerificationUploadStorage>();
            using var bytes = new MemoryStream("%PDF fictional"u8.ToArray());
            var abandoned = await intake.StageAsync(bytes, default);
            bytes.Position = 0;
            var committed = await intake.StageAsync(bytes, default);
            var docId = await h.Db(async db =>
            {
                var doc = await db.VerificationApplications.Where(a => a.Subject == VerificationSubject.Startup).SelectMany(a => a.Documents).FirstAsync();
                doc.StorageKey = committed;
                await db.SaveChangesAsync();
                return doc.Id;
            });
            foreach (var key in new[] { abandoned, committed })
                File.SetLastWriteTimeUtc(Path.Combine(root, "verification-intake", key), h.Clock.UtcNow.AddDays(-1));
            async Task Sweep() => await h.Run<IVerificationFacade, int>(async v => { await v.ProcessEvidenceErasureAsync(); return 0; });
            await Sweep();
            Assert.False(File.Exists(Path.Combine(root, abandoned)));
            var path = Path.Combine(root, committed);
            Assert.True(File.Exists(path));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "verification-intake")));

            var owner = await h.PendingFounder();
            await h.Run<IAccountFacade, Result>(v => v.DeleteAccountAsync(owner, "DELETE"));
            File.Move(path, path + ".saved");
            Directory.CreateDirectory(path); // Simulate storage refusing the delete.
            await Sweep();
            var pending = await h.Db(db => db.VerificationDocuments.SingleAsync(d => d.Id == docId));
            Assert.False(pending.Erased);
            Assert.Equal(committed, pending.StorageKey);
            Directory.Delete(path);
            File.Move(path + ".saved", path);
            await Sweep();
            Assert.False(File.Exists(path));
            Assert.True(await h.Db(db => db.VerificationDocuments.AnyAsync(d => d.Id == docId && d.Erased)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ReviewRequiresCurrentReviewerAndCleanBytes()
    {
        await using var h = await TestHost.CreateAsync();
        var reviewer = await h.Admin("verify");
        var super = await h.Admin("super");
        var app = await h.Db(db => db.VerificationApplications.Include(a => a.Documents).SingleAsync(a => a.Subject == VerificationSubject.Investor));
        async Task<Result<(Stream Content, string FileName)>> Open(Actor actor, int id) =>
            await h.Run<IVerificationFacade, Result<(Stream Content, string FileName)>>(v => v.OpenEvidenceAsync(actor, app.Id, id));
        Assert.True((await Open(await h.Admin("finance"), app.Documents[0].Id)).IsFailure);
        Assert.True((await Open(reviewer with { MfaSatisfied = false }, app.Documents[0].Id)).IsFailure);
        foreach (var d in app.Documents)
        {
            var result = await Open(reviewer, d.Id);
            Assert.True(result.IsSuccess, result.Error.Message);
            result.Value.Content.Dispose();
        }
        Assert.True((await h.Run<IVerificationFacade, Result>(v => v.ApproveAsync(super, app.Id, "Reviewed application"))).IsFailure);
        Assert.True((await h.Run<IVerificationFacade, Result>(v => v.ApproveAsync(reviewer, app.Id, "Reviewed application", DateOnly.FromDateTime(h.Clock.UtcNow)))).IsSuccess);
        Assert.True((await Open(reviewer, app.Documents[0].Id)).IsFailure);
        var expiry = await h.Db(db => db.InvestorProfiles.Where(i => i.Id == app.InvestorId).Select(i => i.VerifiedUntilUtc).SingleAsync());
        Assert.Equal(new DateTime(2026, 11, 7, 18, 0, 0, DateTimeKind.Utc), expiry);
    }

    [Fact]
    public async Task ExpiryBlocksPitchBeforeJobAndCancelsPendingRequests()
    {
        await using var h = await TestHost.CreateAsync();
        var founder = await h.Founder();
        var investor = await h.Imran();
        await h.Db(async db =>
        {
            var s = await db.Startups.SingleAsync(s => s.Id == founder.StartupId);
            s.VerifiedUntilUtc = h.Clock.UtcNow;
            return await db.SaveChangesAsync();
        });
        var pitch = await h.Run<IConnectionRequestFacade, Result<int>>(f => f.SendPitchAsync(founder,
            new(investor.InvestorId!.Value, "Review our funding proposal and pitch package.", Guid.NewGuid().ToString())));
        Assert.Equal(ErrorCodes.NotVerified, pitch.Error.Code);
        await h.Run<IVerificationFacade, int>(async v => { await v.ExpireDueAsync(); return 0; });
        Assert.Equal(VerificationStatus.Expired, await h.Db(db => db.Startups.Where(s => s.Id == founder.StartupId).Select(s => s.VerificationStatus).SingleAsync()));
        Assert.False(await h.Db(db => db.ConnectionRequests.AnyAsync(r => r.StartupId == founder.StartupId && r.Status == RequestStatus.Pending)));
    }
}
