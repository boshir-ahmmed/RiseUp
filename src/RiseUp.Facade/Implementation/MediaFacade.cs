using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Media;


/// <summary>
/// Access rules: profile photos/covers and startup logos/covers are public; live post videos are public; teasers and demos are
/// public only when the founder opted in and the startup is verified; pitch videos, decks and pitch documents are visible to the
/// owning founder and to investors who received a pitch from that startup (documents: only those attached to their pitch).
/// </summary>
public sealed class MediaFacade(IMediaRepository media, IFileStorage storage, IMalwareScanner scanner, IPitchRepository pitches,
    ISavedPostRepository postsByMedia, IStartupRepository startups, IInvestorRepository investors, IUnitOfWork uow, IClock clock) : IMediaFacade
{
    public async Task<Result<MediaStream>> OpenAsync(Actor actor, int mediaId, bool download, CancellationToken ct = default)
    {
        var asset = await media.GetByIdAsync(mediaId, ct);
        if (asset is null) return Guard.NotFound("File");
        var ownerView = actor.IsFounder && asset.OwnerStartupId is int sid && actor.StartupId == sid;
        // Owners may preview their own quarantined uploads; nobody else sees anything that is not scanned clean.
        if (asset.ScanStatus != ScanStatus.Clean && !(ownerView && asset.ScanStatus == ScanStatus.Pending)) return Guard.NotFound("File");
        var allowDownload = false;
        if (!await CanViewAsync(actor, asset, ownerView, d => allowDownload = d, ct)) return Guard.NotFound("File");
        if (download && !allowDownload && !ownerView) return new Error(ErrorCodes.ForbiddenDocument, "The founder has not enabled download for this file.");
        var stream = await storage.OpenAsync(asset.StorageKey, ct);
        if (stream is null) return Guard.NotFound("File");
        return Result.Success(new MediaStream(stream, asset.ContentType, asset.FileName, download));
    }

    private async Task<bool> CanViewAsync(Actor actor, MediaAsset asset, bool owner, Action<bool> setDownload, CancellationToken ct)
    {
        switch (asset.Kind)
        {
            case MediaKind.ProfilePhoto or MediaKind.ProfileLogo or MediaKind.ProfileCover or MediaKind.StartupLogo or MediaKind.StartupCover:
                return true;
            case MediaKind.PostImage or MediaKind.PostVideo:
                return owner || await IsPublicPostMediaAsync(asset.Id, ct);
            case MediaKind.PitchDocument:
            {
                if (owner) { setDownload(true); return true; }
                var doc = await pitches.FindDocumentByMediaAsync(asset.Id, ct);
                if (doc is null || asset.OwnerStartupId is not int s || !await IsRecipientAsync(actor, s, ct)) return false;
                var attached = (await pitches.ListPitchesForPairAsync(s, actor.InvestorId!.Value, ct))
                    .Where(r => r.Status is RequestStatus.Pending or RequestStatus.Accepted).Any(r => r.AttachedDocuments.Contains(doc.Id));
                setDownload(doc.AllowDownload);
                return attached;
            }
        }
        if (asset.Kind == MediaKind.VideoPoster && await postsByMedia.FindPostByMediaAsync(asset.Id, ct) is { } post) return owner || (post.IsLive && await IsVisibleStartupAsync(post.StartupId, ct));
        // Pitch package media: pitch/demo/teaser videos, their posters and the deck.
        var package = await pitches.FindPackageByMediaAsync(asset.Id, ct);
        if (package is null) return owner;
        if (owner) return true;
        var startup = await startups.GetByIdAsync(package.StartupId, ct);
        if (startup is null || !startup.IsActiveVerifiedAt(clock.UtcNow)) return false;
        var isTeaser = asset.Id == package.TeaserVideoId || asset.Id == package.TeaserPosterId;
        var isDemo = asset.Id == package.DemoVideoId || asset.Id == package.DemoPosterId;
        if ((isTeaser && package.PublicTeaser) || (isDemo && package.PublicDemo)) return true;
        return await IsRecipientAsync(actor, package.StartupId, ct);
    }

    private async Task<bool> IsPublicPostMediaAsync(int id, CancellationToken ct) =>
        await postsByMedia.FindPostByMediaAsync(id, ct) is { IsLive: true } post && await IsVisibleStartupAsync(post.StartupId, ct);

    private async Task<bool> IsVisibleStartupAsync(int id, CancellationToken ct) =>
        await startups.GetByIdAsync(id, ct) is { IsSuspended: false, IsDeleted: false };

    /// <summary>An investor with a pending or accepted pitch from this startup.</summary>
    private async Task<bool> IsRecipientAsync(Actor actor, int startupId, CancellationToken ct)
    {
        if (!actor.IsInvestor) return false;
        var me = await investors.GetByIdAsync(actor.InvestorId!.Value, ct);
        if (me is null || me.IsDeleted || me.IsSuspended) return false;
        return (await pitches.ListPitchesForPairAsync(startupId, me.Id, ct)).Any(r => r.Status is RequestStatus.Pending or RequestStatus.Accepted);
    }

    public async Task<int> ProcessScanQueueAsync(CancellationToken ct = default)
    {
        var done = 0;
        foreach (var asset in await media.ListPendingScanAsync(25, ct))
        {
            await using var stream = await storage.OpenAsync(asset.StorageKey, ct);
            if (stream is null) { asset.ScanStatus = ScanStatus.Infected; asset.DeletedAtUtc = clock.UtcNow; done++; continue; }
            var verdict = await scanner.ScanAsync(stream, ct);
            if (verdict == ScanVerdict.Unavailable) break;   // try again next tick; stays quarantined
            asset.ScanStatus = verdict == ScanVerdict.Clean ? ScanStatus.Clean : ScanStatus.Infected;
            if (asset.ScanStatus == ScanStatus.Infected) { asset.DeletedAtUtc = clock.UtcNow; await storage.DeleteAsync(asset.StorageKey, ct); }
            done++;
        }
        if (done > 0) await uow.SaveChangesAsync(ct);
        return done;
    }
}
