using Microsoft.AspNetCore.Identity;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

/// <summary>PBKDF2 hashing from ASP.NET Core Identity. Replaces the legacy plaintext password predicate.</summary>
public sealed class PasswordHasherAdapter : RiseUp.Entities.Abstractions.IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Subject = new();

    public string Hash(string password) => Hasher.HashPassword(Subject, password);

    public bool Verify(string hash, string password) =>
        !string.IsNullOrEmpty(hash) && Hasher.VerifyHashedPassword(Subject, hash, password) != PasswordVerificationResult.Failed;
}
