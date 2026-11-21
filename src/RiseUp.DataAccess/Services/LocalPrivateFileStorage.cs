using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

public sealed class StorageOptions
{
    /// <summary>Private folder outside wwwroot. Production should use private object storage.</summary>
    public string RootPath { get; set; } = "App_Data/private";
}

/// <summary>Opaque generated keys; original names never become paths (prevents traversal and guessing).</summary>
public sealed class LocalPrivateFileStorage(IOptions<StorageOptions> options) : IFileStorage, IVerificationUploadStorage
{
    private string Root => Path.GetFullPath(options.Value.RootPath);

    private static string NewKey() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    private string Receipts => Path.Combine(Root, "verification-intake");

    public Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default) =>
        WriteAsync(NewKey(), content, ct);

    public async Task<string> StageAsync(Stream content, CancellationToken ct)
    {
        var key = NewKey();
        Directory.CreateDirectory(Receipts);
        using (File.Create(Path.Combine(Receipts, key))) { }
        return await WriteAsync(key, content, ct);
    }

    public IEnumerable<string> ListStaleReceipts(DateTime olderThanUtc)
    {
        if (!Directory.Exists(Receipts)) yield break;
        foreach (var path in Directory.EnumerateFiles(Receipts))
        {
            var key = Path.GetFileName(path);
            if (IsSafeKey(key) && File.GetLastWriteTimeUtc(path) < olderThanUtc) yield return key;
        }
    }

    public void Complete(string storageKey)
    {
        if (IsSafeKey(storageKey)) File.Delete(Path.Combine(Receipts, storageKey));
    }

    private async Task<string> WriteAsync(string key, Stream content, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, key);
        try
        {
            await using var file = File.Create(path);
            await content.CopyToAsync(file, ct);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return key;
    }

    public Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default)
    {
        if (!IsSafeKey(storageKey)) return Task.FromResult<Stream?>(null);
        var path = Path.Combine(Root, storageKey);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public async Task RestoreIfMissingAsync(string storageKey, string bundledFilePath, CancellationToken ct = default)
    {
        if (!IsSafeKey(storageKey)) throw new ArgumentException("Invalid media key.", nameof(storageKey));
        var target = Path.Combine(Root, storageKey);
        if (File.Exists(target)) return;

        Directory.CreateDirectory(Root);
        var staging = Path.Combine(Root, NewKey() + ".tmp");
        try
        {
            await using (var source = File.OpenRead(bundledFilePath))
            await using (var destination = File.Create(staging))
                await source.CopyToAsync(destination, ct);

            // Expose only a complete file. Another seed process may have restored it first.
            try { File.Move(staging, target); }
            catch (IOException) when (File.Exists(target)) { }
        }
        finally { File.Delete(staging); }
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (IsSafeKey(storageKey))
        {
            var path = Path.Combine(Root, storageKey);
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static bool IsSafeKey(string key) => key.Length == 32 && key.All(Uri.IsHexDigit);
}
