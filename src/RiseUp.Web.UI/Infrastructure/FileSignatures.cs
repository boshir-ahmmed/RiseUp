namespace RiseUp.Web.UI.Infrastructure;

/// <summary>Extension allow-list plus magic-byte check (SR-13, NFR-19). Malware scanning happens after upload (quarantine).</summary>
public static class FileSignatures
{
    private sealed record Sig(int Offset, byte[] Bytes);

    private static readonly Dictionary<string, Sig[]> Signatures = new()
    {
        [".pdf"] = [new(0, [0x25, 0x50, 0x44, 0x46])],
        [".pptx"] = [new(0, [0x50, 0x4B, 0x03, 0x04])],
        [".xlsx"] = [new(0, [0x50, 0x4B, 0x03, 0x04])],
        [".png"] = [new(0, [0x89, 0x50, 0x4E, 0x47])],
        [".jpg"] = [new(0, [0xFF, 0xD8, 0xFF])],
        [".jpeg"] = [new(0, [0xFF, 0xD8, 0xFF])],
        [".webp"] = [new(0, [0x52, 0x49, 0x46, 0x46])],
        [".mp4"] = [new(4, "ftyp"u8.ToArray())],
        [".mov"] = [new(4, "ftyp"u8.ToArray()), new(4, "moov"u8.ToArray()), new(4, "wide"u8.ToArray())],
        [".webm"] = [new(0, [0x1A, 0x45, 0xDF, 0xA3])],
    };

    /// <summary>Content type implied by an extension whose signature has been verified.</summary>
    public static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"] = "application/pdf",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp",
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".webm"] = "video/webm",
    };

    public static async Task<bool> LooksLikeAsync(IFormFile file, IReadOnlyCollection<string> allowedExtensions)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext) || !Signatures.TryGetValue(ext, out var sigs)) return false;
        var header = new byte[16];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(header);
        return sigs.Any(sig => read >= sig.Offset + sig.Bytes.Length && header.AsSpan(sig.Offset, sig.Bytes.Length).SequenceEqual(sig.Bytes));
    }
}
