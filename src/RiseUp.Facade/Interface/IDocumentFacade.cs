using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.ConfidentialDocuments;

public interface IDocumentFacade
{
    Task<IReadOnlyList<DocumentDto>> ListOwnAsync(Actor owner, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentDto>> ListSharedWithMeAsync(Actor investor, CancellationToken ct = default);
    Task<Result<int>> RegisterUploadAsync(Actor owner, string fileName, DocumentKind kind, long sizeBytes, string storageKey, CancellationToken ct = default);
    Task<Result> GrantAsync(Actor owner, int documentId, int investorId, CancellationToken ct = default);
    Task<Result> RevokeAsync(Actor owner, int documentId, int investorId, CancellationToken ct = default);
    /// <summary>Called on EVERY download; returns the private storage key only when authorised (FR-104/105).</summary>
    Task<Result<(string StorageKey, string FileName)>> AuthorizeDownloadAsync(Actor actor, int documentId, CancellationToken ct = default);
    /// <summary>Background job: scans quarantined files; infected files are deleted, outages leave files quarantined.</summary>
    Task<int> ProcessScanQueueAsync(CancellationToken ct = default);
}
