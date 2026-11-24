using System.Security.Cryptography;
using System.Text;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Common;

internal static class SecureTokens
{
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#";

    public static string NewUrlToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static string NewPassword()
    {
        string password;
        do
        {
            password = RandomNumberGenerator.GetString(PasswordAlphabet, 14);
        } while (!PasswordPolicy.IsValid(password));
        return password;
    }
}
