using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

public sealed class StorageOptions
{
    /// <summary>Private folder outside wwwroot. Production should use private object storage.</summary>
    public string RootPath { get; set; } = "App_Data/private";
}

/// <summary>Opaque generated keys; original names never become paths (prevents traversal and guessing).</summary>
public sealed class LocalPrivateFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private string Root => Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Root);
        var key = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await using var file = File.Create(Path.Combine(Root, key));
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default)
    {
        if (!IsSafeKey(storageKey)) return Task.FromResult<Stream?>(null);
        var path = Path.Combine(Root, storageKey);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (IsSafeKey(storageKey))
        {
            var path = Path.Combine(Root, storageKey);
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static bool IsSafeKey(string key) => key.Length == 32 && key.All(Uri.IsHexDigit);
}
