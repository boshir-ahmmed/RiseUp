using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Media;


/// <summary>Validates, scans and registers uploads for every feature. Rejected or infected files are deleted immediately.</summary>
public sealed class MediaIntake(IMediaRepository media, IFileStorage storage, IMalwareScanner scanner, ILogger<MediaIntake> logger)
{
    private sealed record Rule(string[] Types, long MaxBytes, int? MaxSeconds, string Label);

    private static readonly Dictionary<MediaKind, Rule> Rules = new()
    {
        [MediaKind.ProfilePhoto] = new(MediaTypes.Images, BusinessRules.ProfilePhotoMaxBytes, null, "Profile photos"),
        [MediaKind.ProfileCover] = new(MediaTypes.Images, BusinessRules.ProfileCoverMaxBytes, null, "Cover photos"),
        [MediaKind.StartupLogo] = new(MediaTypes.Images, BusinessRules.LogoMaxBytes, null, "Logos"),
        [MediaKind.StartupCover] = new(MediaTypes.Images, BusinessRules.ProfileCoverMaxBytes, null, "Cover images"),
        [MediaKind.VideoPoster] = new(MediaTypes.Images, 2 * 1024 * 1024, null, "Poster frames"),
        [MediaKind.PitchVideo] = new(MediaTypes.Videos, BusinessRules.VideoMaxBytes, BusinessRules.PitchVideoMaxSeconds, "Pitch videos"),
        [MediaKind.DemoVideo] = new(MediaTypes.Videos, BusinessRules.VideoMaxBytes, BusinessRules.DemoVideoMaxSeconds, "Product demos"),
        [MediaKind.TeaserVideo] = new(MediaTypes.Videos, BusinessRules.VideoMaxBytes, BusinessRules.TeaserMaxSeconds, "Teasers"),
        [MediaKind.PostVideo] = new(MediaTypes.Videos, BusinessRules.PostVideoMaxBytes, BusinessRules.PostVideoMaxSeconds, "Post videos"),
        [MediaKind.PitchDeck] = new([MediaTypes.Pdf, MediaTypes.Pptx], BusinessRules.PitchDeckMaxBytes, null, "Pitch decks"),
        [MediaKind.PitchDocument] = new([MediaTypes.Pdf, MediaTypes.Pptx, MediaTypes.Xlsx], BusinessRules.PitchDocumentMaxBytes, null, "Pitch documents"),
    };

    public async Task<Result<MediaAsset>> AcceptAsync(MediaKind kind, UploadedFile file, int? startupId, int? accountId, int? teamAccountId, CancellationToken ct = default)
    {
        var rule = Rules[kind];
        string? problem = null;
        if (!rule.Types.Contains(file.ContentType)) problem = $"{rule.Label} must be {Describe(rule.Types)}.";
        else if (file.SizeBytes <= 0 || file.SizeBytes > rule.MaxBytes) problem = $"{rule.Label} are limited to {rule.MaxBytes / (1024 * 1024)} MB.";
        else if (rule.MaxSeconds is int max)
        {
            if (file.DurationSeconds is not int secs || secs <= 0) problem = "We couldn't read the video length. Export it as MP4 (H.264) and try again.";
            else if (secs > max) problem = $"{rule.Label} can be at most {max / 60}:{max % 60:00} long (this one is {secs / 60}:{secs % 60:00}).";
        }
        if (problem is not null)
        {
            await storage.DeleteAsync(file.StorageKey, ct);
            return new Error(ErrorCodes.InvalidMedia, problem);
        }

        var status = ScanStatus.Pending;
        try
        {
            await using var stream = await storage.OpenAsync(file.StorageKey, ct);
            if (stream is null) return new Error(ErrorCodes.InvalidMedia, "The upload could not be read. Please try again.");
            status = await scanner.ScanAsync(stream, ct) switch { ScanVerdict.Clean => ScanStatus.Clean, ScanVerdict.Infected => ScanStatus.Infected, _ => ScanStatus.Pending };
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Scanner outage: keep the file quarantined; the background job retries. Never release unscanned.
            logger.LogWarning("Malware scan deferred for {Kind}: {Error}", kind, ex.GetType().Name);
        }
        if (status == ScanStatus.Infected)
        {
            await storage.DeleteAsync(file.StorageKey, ct);
            return new Error(ErrorCodes.MediaInfected, "The file failed the malware scan and was deleted.");
        }
        var asset = new MediaAsset
        {
            Kind = kind, StorageKey = file.StorageKey, ContentType = file.ContentType, FileName = Safe(file.FileName), SizeBytes = file.SizeBytes,
            DurationSeconds = file.DurationSeconds, OwnerStartupId = startupId, OwnerAccountId = accountId, OwnerTeamAccountId = teamAccountId, ScanStatus = status,
        };
        await media.AddAsync(asset, ct);
        return Result.Success(asset);
    }

    /// <summary>Soft-deletes a replaced asset and removes its bytes. Safe to call with null.</summary>
    public async Task DiscardAsync(int? mediaId, DateTime nowUtc, CancellationToken ct = default)
    {
        if (mediaId is not int id || await media.GetByIdAsync(id, ct) is not { } asset) return;
        asset.DeletedAtUtc = nowUtc;
        await storage.DeleteAsync(asset.StorageKey, ct);
    }

    private static string Describe(string[] types) => string.Join(", ", types.Select(t => t switch
    {
        "image/jpeg" => "JPG", "image/png" => "PNG", "image/webp" => "WebP", "video/mp4" => "MP4", "video/webm" => "WebM", "video/quicktime" => "MOV",
        MediaTypes.Pdf => "PDF", MediaTypes.Pptx => "PPTX", MediaTypes.Xlsx => "XLSX", _ => t,
    }));

    private static string Safe(string name)
    {
        var n = Path.GetFileName(name ?? "file");
        n = new string(n.Where(c => !char.IsControl(c) && c is not ('"' or '\\' or '/')).ToArray());
        return n.Length == 0 ? "file" : n.Length > 200 ? n[^200..] : n;
    }
}
