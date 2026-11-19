using RiseUp.Entities.Common;

namespace RiseUp.Entities.Media;

/// <summary>
/// Any uploaded image, video or document. Stored privately under an opaque key; served only through the media
/// endpoint after the Facade re-authorises the caller. New uploads stay quarantined until scanned clean (SR-13).
/// </summary>
public class MediaAsset : BaseEntity
{
    public MediaKind Kind { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public int? OwnerStartupId { get; set; }
    public int? OwnerAccountId { get; set; }
    public int? OwnerTeamAccountId { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public bool IsImage => Kind is MediaKind.ProfilePhoto or MediaKind.ProfileCover or MediaKind.StartupLogo or MediaKind.StartupCover or MediaKind.VideoPoster;
    public bool IsVideo => Kind is MediaKind.PitchVideo or MediaKind.DemoVideo or MediaKind.TeaserVideo or MediaKind.PostVideo;
}
