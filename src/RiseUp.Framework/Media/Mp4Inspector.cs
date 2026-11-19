namespace RiseUp.Framework.Media;

/// <summary>
/// Reads the duration of an ISO-BMFF video (MP4 / MOV) from its <c>moov/mvhd</c> box without decoding.
/// Used to enforce pitch/demo/teaser length limits server-side. Returns null when the box is not found.
/// </summary>
public static class Mp4Inspector
{
    public static double? TryReadDurationSeconds(Stream stream)
    {
        if (!stream.CanSeek) return null;
        try
        {
            stream.Position = 0;
            return Walk(stream, 0, stream.Length, depth: 0);
        }
        catch (IOException) { return null; }
        catch (ArgumentException) { return null; }
        finally { if (stream.CanSeek) stream.Position = 0; }
    }

    private static double? Walk(Stream s, long start, long end, int depth)
    {
        if (depth > 4) return null;
        var pos = start;
        Span<byte> head = stackalloc byte[16];
        while (pos + 8 <= end)
        {
            s.Position = pos;
            if (s.Read(head[..8]) < 8) return null;
            long size = ReadU32(head[..4]);
            var type = System.Text.Encoding.ASCII.GetString(head.Slice(4, 4));
            var headerLen = 8;
            if (size == 1)
            {
                if (s.Read(head.Slice(8, 8)) < 8) return null;
                size = (long)ReadU64(head.Slice(8, 8)); headerLen = 16;
            }
            else if (size == 0) size = end - pos;
            if (size < headerLen || pos + size > end) return null;
            if (type == "moov") return Walk(s, pos + headerLen, pos + size, depth + 1);
            if (type == "mvhd")
            {
                s.Position = pos + headerLen;
                Span<byte> b = stackalloc byte[32];
                if (s.Read(b) < 32) return null;
                var version = b[0];
                if (version == 1)
                {
                    var timescale = ReadU32(b.Slice(20, 4));
                    s.Position = pos + headerLen + 24;
                    Span<byte> d = stackalloc byte[8];
                    if (s.Read(d) < 8) return null;
                    return timescale == 0 ? null : ReadU64(d) / (double)timescale;
                }
                var ts = ReadU32(b.Slice(12, 4));
                var dur = ReadU32(b.Slice(16, 4));
                return ts == 0 ? null : dur / (double)ts;
            }
            pos += size;
        }
        return null;
    }

    private static uint ReadU32(ReadOnlySpan<byte> b) => (uint)(b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3]);
    private static ulong ReadU64(ReadOnlySpan<byte> b) => (ulong)ReadU32(b[..4]) << 32 | ReadU32(b.Slice(4, 4));
}
