using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

/// <summary>Streams private files to the configured internal clamd service using INSTREAM.</summary>
public sealed class ClamAvScanner(string? host, int port) : IMalwareScanner
{
    public async Task<ScanVerdict> ScanAsync(Stream content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(host)) return ScanVerdict.Unavailable;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, token);
            using var stream = client.GetStream();
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), token);
            var buffer = new byte[65536];
            var size = new byte[4];
            int count;
            while ((count = await content.ReadAsync(buffer, token)) > 0)
            {
                BinaryPrimitives.WriteInt32BigEndian(size, count);
                await stream.WriteAsync(size, token);
                await stream.WriteAsync(buffer.AsMemory(0, count), token);
            }
            await stream.WriteAsync(new byte[4], token);
            var response = new List<byte>();
            while (response.Count < 4096)
            {
                count = await stream.ReadAsync(buffer, token);
                if (count == 0) break;
                var end = Array.IndexOf(buffer, (byte)0, 0, count);
                response.AddRange(buffer.AsSpan(0, end < 0 ? count : end).ToArray());
                if (end < 0) continue;
                var reply = Encoding.UTF8.GetString(response.ToArray());
                if (reply == "stream: OK") return ScanVerdict.Clean;
                return reply.StartsWith("stream: ", StringComparison.Ordinal) && reply.EndsWith(" FOUND", StringComparison.Ordinal)
                    ? ScanVerdict.Infected : ScanVerdict.Unavailable;
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is IOException or SocketException or OperationCanceledException) { }
        return ScanVerdict.Unavailable;
    }
}
