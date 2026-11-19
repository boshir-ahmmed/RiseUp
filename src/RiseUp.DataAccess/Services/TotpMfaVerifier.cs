using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;
using RiseUp.Framework.Time;

namespace RiseUp.DataAccess.Services;

public sealed class MfaOptions
{
    /// <summary>Development only: accept any 6-digit code for seeded admins without an enrolled secret.</summary>
    public bool AllowDemoCodes { get; set; }
}

/// <summary>RFC 6238 TOTP (SHA-1, 30 s, 6 digits, ±1 step).</summary>
public sealed class TotpMfaVerifier(IOptions<MfaOptions> options, IClock clock) : IMfaVerifier
{
    public bool Verify(string? secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit)) return false;
        if (string.IsNullOrEmpty(secret)) return options.Value.AllowDemoCodes;
        var key = Convert.FromBase64String(secret);
        var step = new DateTimeOffset(clock.UtcNow).ToUnixTimeSeconds() / 30;
        for (var offset = -1; offset <= 1; offset++)
            if (Compute(key, step + offset) == code) return true;
        return false;
    }

    private static string Compute(byte[] key, long counter)
    {
        var bytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
#pragma warning disable CA5350 // RFC 6238 TOTP mandates HMAC-SHA1 for authenticator-app compatibility.
        using var hmac = new HMACSHA1(key);
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(bytes);
        var o = hash[^1] & 0x0F;
        var value = ((hash[o] & 0x7F) << 24) | (hash[o + 1] << 16) | (hash[o + 2] << 8) | hash[o + 3];
        return (value % 1_000_000).ToString("D6");
    }
}
