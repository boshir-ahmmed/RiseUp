using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Media;
using RiseUp.Framework.Media;

namespace RiseUp.Web.UI.Infrastructure;

/// <summary>
/// Stores an upload after the extension + signature check and reads video duration server-side (MP4/MOV headers).
/// The Facade (<see cref="MediaIntake"/>) then applies the business limits, scans and registers it.
/// </summary>
public static class UploadSaver
{
    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    public static readonly string[] VideoExtensions = [".mp4", ".mov", ".webm"];
    public static readonly string[] DeckExtensions = [".pdf", ".pptx"];
    public static readonly string[] DocumentExtensions = [".pdf", ".pptx", ".xlsx"];

    public static async Task<(UploadedFile? File, string? Error)> SaveAsync(IFormFile? file, IReadOnlyCollection<string> allowed, IFileStorage storage,
        double? clientDurationSeconds, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return (null, "Choose a file to upload.");
        if (!await FileSignatures.LooksLikeAsync(file, allowed))
            return (null, $"{Path.GetFileName(file.FileName)} is not an allowed file type ({string.Join(", ", allowed.Select(a => a.TrimStart('.').ToUpperInvariant()))}).");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        string key;
        await using (var s = file.OpenReadStream()) key = await storage.SaveAsync(s, file.FileName, ct);
        int? duration = null;
        if (ext is ".mp4" or ".mov")
        {
            await using var stored = await storage.OpenAsync(key, ct);
            if (stored is not null && Mp4Inspector.TryReadDurationSeconds(stored) is double secs) duration = (int)Math.Ceiling(secs - 0.05);
        }
        else if (ext == ".webm" && clientDurationSeconds is double c && c > 0 && c < 24 * 3600) duration = (int)Math.Ceiling(c);
        return (new UploadedFile(key, Path.GetFileName(file.FileName), FileSignatures.ContentTypes[ext], file.Length, duration), null);
    }
}
