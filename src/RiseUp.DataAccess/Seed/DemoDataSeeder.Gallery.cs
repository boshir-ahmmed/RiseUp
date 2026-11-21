using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Seed;

public sealed partial class DemoDataSeeder
{
    private static readonly (string Email, string Portrait)[] Portraits =
    [
        ("farhan@nodifreight.example", "farhan"), ("hello@takaledger.example", "nabila"),
        ("team@shasthyolink.example", "mahin"), ("sabbir@pathshalabyte.example", "sabbir"),
        ("ishrat@sobujgrid.example", "ishrat"), ("rk@haatbazar.example", "rezaul"),
        ("tasnim@juteloop.example", "tasnim"), ("ayesha@krishicloud.example", "ayesha"),
        ("sadia@padmavc.example", "sadia"), ("imran@angel.example", "imran"),
        ("nusrat@meghnacap.example", "nusrat"), ("arif@bengalangels.example", "arif"),
        ("tanvir@deltagp.example", "tanvir"), ("mehnaz@angel.example", "mehnaz"),
        ("super@riseup.example", "lamia"), ("verify@riseup.example", "rashed"),
        ("moderator@riseup.example", "priya"), ("finance@riseup.example", "kamal")
    ];

    private static readonly Dictionary<string, string> StartupCovers = new()
    {
        ["farhan"] = "freight", ["nabila"] = "grocery", ["mahin"] = "clinic", ["sabbir"] = "school",
        ["ishrat"] = "solar", ["rezaul"] = "market", ["tasnim"] = "jute", ["ayesha"] = "farm"
    };

    private sealed record FieldStory(string Owner, string Key, string Title, string Caption, string ReelTitle, string ReelCaption);
    private static readonly FieldStory[] Stories =
    [
        new("farhan", "freight", "Morning shift at the freight yard",
            "Our dispatch team pairs outbound loads with the next available return trip at Chattogram.",
            "Before the first truck leaves", "A look at the three checks our dispatchers make before a morning handover."),
        new("nabila", "grocery", "A ledger that fits behind the counter",
            "We spent the afternoon with neighbourhood shopkeepers, learning how they close the till and record credit sales.",
            "Closing time at a neighbourhood shop", "From checking the shelf to recording the last sale, here is the evening routine."),
        new("ishrat", "solar", "A maintenance morning on the chars",
            "The field team checked panel rows and meter readings before the afternoon demand peak.",
            "Walking a river-island solar array", "Follow a field engineer through the morning maintenance checklist."),
        new("tasnim", "jute", "From jute fibre to a finished tray",
            "A trial batch from our workshop is ready for packing tests with local exporters.",
            "Inside the jute workshop", "See where fibre preparation, pressing and the final quality check fit into a production run.")
    ];

    private void ValidateBundledMedia()
    {
        IEnumerable<string> pitchFiles = Packages.SelectMany(p => new[] { "pitch", "demo", "teaser" }
            .SelectMany(slot => new[] { $"{p.Key}-{slot}.mp4", $"{p.Key}-{slot}.jpg" }));
        IEnumerable<string> galleryFiles = Portraits.Select(p => $"portrait-{p.Portrait}.jpg")
            .Concat(Stories.SelectMany(s => new[] { $"field-{s.Key}.jpg", $"reel-{s.Key}.mp4", $"reel-{s.Key}.jpg" }));
        var branding = StartupCovers.SelectMany(s => new[] { $"logo-{s.Key}.jpg", $"field-{s.Value}.jpg" }).Append("portrait-rafi.jpg");
        foreach (var name in pitchFiles.Concat(galleryFiles).Concat(branding))
            if (!File.Exists(Path.Combine(seedOptions.Value.DemoMediaPath, name)))
                throw new FileNotFoundException($"Demo media is missing. Restore DemoMedia from the checkout: {name}");
    }

    private async Task SeedGalleryAsync(CancellationToken ct)
    {
        // The original filename survives edits and soft deletion. A second import must respect both.
        var originalAssets = (await db.MediaAssets.AsNoTracking().Where(a => a.FileName.StartsWith("portrait-") ||
            a.FileName.StartsWith("field-") || a.FileName.StartsWith("logo-") || a.FileName.StartsWith("reel-"))
            .OrderBy(a => a.Id).ToListAsync(ct))
            .ToLookup(a => (a.OwnerAccountId, a.OwnerTeamAccountId, a.Kind, a.FileName));

        async Task<MediaAsset?> ExistingAsync(string name, MediaKind kind, int? owner, int? team = null)
        {
            var original = originalAssets[(owner, team, kind, name)].FirstOrDefault();
            if (original is { DeletedAtUtc: null, ScanStatus: ScanStatus.Clean })
            {
                var source = new FileInfo(Path.Combine(seedOptions.Value.DemoMediaPath, name));
                if (source.Exists && source.Length == original.SizeBytes)
                    await storage.RestoreIfMissingAsync(original.StorageKey, source.FullName, ct);
            }
            return original;
        }

        var emails = Portraits.Select(p => p.Email.ToUpperInvariant()).ToArray();
        var owners = await db.UserAccounts.Where(u => emails.Contains(u.NormalizedEmail))
            .ToDictionaryAsync(u => u.NormalizedEmail, u => u.Id, ct);
        var ids = owners.Values.ToList();
        var profiles = await db.MemberProfiles.Where(p => p.AccountId.HasValue && ids.Contains(p.AccountId.Value))
            .ToDictionaryAsync(p => p.AccountId!.Value, ct);
        var startups = await db.Startups.Where(s => ids.Contains(s.OwnerAccountId)).ToDictionaryAsync(s => s.OwnerAccountId, ct);

        foreach (var (email, portrait) in Portraits)
        {
            if (!owners.TryGetValue(email.ToUpperInvariant(), out var owner)) continue;
            startups.TryGetValue(owner, out var startup);
            var name = $"portrait-{portrait}.jpg";
            if (await ExistingAsync(name, MediaKind.ProfilePhoto, owner) is null &&
                profiles.TryGetValue(owner, out var profile) && profile.PhotoMediaId is null)
                profile.PhotoMediaId = (await LoadAsync(name, MediaKind.ProfilePhoto, startup?.Id, owner, ct)).Id;

            if (startup is null || !StartupCovers.TryGetValue(portrait, out var coverKey)) continue;
            var logo = $"logo-{portrait}.jpg";
            if (await ExistingAsync(logo, MediaKind.StartupLogo, owner) is null && startup.LogoMediaId is null)
                startup.LogoMediaId = (await LoadAsync(logo, MediaKind.StartupLogo, startup.Id, owner, ct)).Id;
            var field = $"field-{coverKey}.jpg";
            if (await ExistingAsync(field, MediaKind.StartupCover, owner) is null && startup.CoverMediaId is null)
                startup.CoverMediaId = (await LoadAsync(field, MediaKind.StartupCover, startup.Id, owner, ct)).Id;

            var story = Stories.FirstOrDefault(s => s.Owner == portrait);
            if (story is null) continue;
            if (await ExistingAsync(field, MediaKind.PostImage, owner) is null)
            {
                var photo = await LoadAsync(field, MediaKind.PostImage, startup.Id, owner, ct);
                db.Posts.Add(Publication(startup.Id, PostCategory.Products, story.Title, story.Caption, photo.Id));
            }
            var clip = $"reel-{story.Key}.mp4";
            var reelVideo = await ExistingAsync(clip, MediaKind.PostVideo, owner);
            var posterName = $"reel-{story.Key}.jpg";
            var reelPoster = await ExistingAsync(posterName, MediaKind.VideoPoster, owner);
            if (reelVideo is null)
            {
                var video = await LoadAsync(clip, MediaKind.PostVideo, startup.Id, owner, ct);
                var poster = reelPoster ?? await LoadAsync(posterName, MediaKind.VideoPoster, startup.Id, owner, ct);
                db.Posts.Add(Publication(startup.Id, PostCategory.Reel, story.ReelTitle, story.ReelCaption, poster.Id, video));
            }
        }
        var team = await db.TeamAccounts.SingleOrDefaultAsync(t => t.Username == "rafi.ops" && t.Email == "rafi@nodifreight.example", ct);
        if (team is not null && await ExistingAsync("portrait-rafi.jpg", MediaKind.ProfilePhoto, null, team.Id) is null &&
            await db.MemberProfiles.SingleOrDefaultAsync(p => p.TeamAccountId == team.Id, ct) is { PhotoMediaId: null } member)
        {
            member.PhotoMediaId = (await LoadAsync("portrait-rafi.jpg", MediaKind.ProfilePhoto, null, null, ct, team.Id)).Id;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<MediaAsset> LoadAsync(string name, MediaKind kind, int? startup, int? owner, CancellationToken ct, int? teamId = null)
    {
        var video = kind == MediaKind.PostVideo;
        var asset = await ImportAsync(name, kind, video ? "video/mp4" : "image/jpeg", video ? 9 : null, startup, owner, ct)
            ?? throw new FileNotFoundException($"Missing bundled demo file: {name}");
        asset.OwnerTeamAccountId = teamId;
        await db.SaveChangesAsync(ct);
        return asset;
    }

    private StartupPost Publication(int startup, PostCategory category, string title, string body, int poster, MediaAsset? video = null) =>
        new() { StartupId = startup, Category = category, LiveTitle = title, LiveBody = body,
            PendingState = ModerationState.Approved, PublishedAtUtc = clock.UtcNow.AddDays(-2),
            PosterMediaId = poster, VideoMediaId = video?.Id, VideoSeconds = video?.DurationSeconds };
}
