using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.ConfidentialDocuments;

public sealed class DocumentFacade(IDocumentRepository documents, IInvestorRepository investors, IUnitOfWork uow, IClock clock, ActivityRecorder activity,
    IFileStorage storage, IMalwareScanner scanner) : IDocumentFacade
{
    public async Task<int> ProcessScanQueueAsync(CancellationToken ct = default)
    {
        var processed = 0;
        foreach (var d in await documents.ListPendingScanAsync(50, ct))
        {
            await using var stream = await storage.OpenAsync(d.StorageKey, ct);
            if (stream is null) continue;
            var verdict = await scanner.ScanAsync(stream, ct);
            if (verdict == ScanVerdict.Unavailable) continue;   // stay quarantined
            d.ScanStatus = verdict == ScanVerdict.Clean ? ScanStatus.Clean : ScanStatus.Infected;
            if (verdict == ScanVerdict.Infected) await storage.DeleteAsync(d.StorageKey, ct);
            processed++;
        }
        if (processed > 0) await uow.SaveChangesAsync(ct);
        return processed;
    }

    public async Task<IReadOnlyList<DocumentDto>> ListOwnAsync(Actor owner, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return [];
        var result = new List<DocumentDto>();
        foreach (var d in await documents.ListForStartupAsync(owner.StartupId.Value, ct))
        {
            var grants = new List<int>();
            foreach (var i in await investors.ListActiveVerifiedAsync(500, ct))
                if (await documents.GetActiveGrantAsync(d.Id, i.Id, ct) is not null) grants.Add(i.Id);
            result.Add(new DocumentDto(d.Id, d.FileName, d.Kind, d.SizeBytes, d.ScanStatus, grants));
        }
        return result;
    }

    public async Task<IReadOnlyList<DocumentDto>> ListSharedWithMeAsync(Actor investor, CancellationToken ct = default)
    {
        if (!investor.IsInvestor) return [];
        var result = new List<DocumentDto>();
        foreach (var g in await documents.ListActiveGrantsForInvestorAsync(investor.InvestorId!.Value, ct))
            if (await documents.GetByIdAsync(g.DocumentId, ct) is { ScanStatus: ScanStatus.Clean } d)
                result.Add(new DocumentDto(d.Id, d.FileName, d.Kind, d.SizeBytes, d.ScanStatus, [investor.InvestorId.Value]));
        return result;
    }

    /// <summary>FR-030: one pitch deck (PDF/PPTX ≤ 20 MB) and up to 10 supporting PDFs (≤ 10 MB). Files start quarantined.</summary>
    public async Task<Result<int>> RegisterUploadAsync(Actor owner, string fileName, DocumentKind kind, long sizeBytes, string storageKey, CancellationToken ct = default)
    {
        if (!owner.IsFounder || owner.StartupId is null) return Guard.Forbidden();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var existing = await documents.ListForStartupAsync(owner.StartupId.Value, ct);
        if (kind == DocumentKind.PitchDeck)
        {
            if (ext is not (".pdf" or ".pptx")) return new Error(ErrorCodes.Validation, "Pitch decks must be PDF or PPTX.");
            if (sizeBytes > BusinessRules.PitchDeckMaxBytes) return new Error(ErrorCodes.Validation, "Pitch decks are limited to 20 MB.");
            if (existing.Any(d => d.Kind == DocumentKind.PitchDeck)) return new Error(ErrorCodes.Conflict, "Only one pitch deck is allowed. Delete the current deck first.");
        }
        else
        {
            if (ext != ".pdf") return new Error(ErrorCodes.Validation, "Supporting documents must be PDF.");
            if (sizeBytes > BusinessRules.SupportingDocMaxBytes) return new Error(ErrorCodes.Validation, "Supporting documents are limited to 10 MB.");
            if (existing.Count(d => d.Kind == DocumentKind.Supporting) >= BusinessRules.SupportingDocMaxCount) return new Error(ErrorCodes.Conflict, "Maximum of 10 supporting documents reached.");
        }
        var doc = new PrivateDocument { StartupId = owner.StartupId.Value, FileName = Path.GetFileName(fileName), Kind = kind, SizeBytes = sizeBytes, StorageKey = storageKey, ScanStatus = ScanStatus.Pending };
        await documents.AddAsync(doc, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(doc.Id, "Uploaded to quarantine. Sharing unlocks once the malware scan is clean.");
    }

    public async Task<Result> GrantAsync(Actor owner, int documentId, int investorId, CancellationToken ct = default)
    {
        var d = await documents.GetByIdAsync(documentId, ct);
        if (d is null || !owner.IsFounder || owner.StartupId != d.StartupId) return Result.Failure(Guard.Forbidden());
        if (d.ScanStatus != ScanStatus.Clean) return Result.Failure(ErrorCodes.UploadPendingScan, "This file is still being scanned.");
        var investor = await investors.GetByIdAsync(investorId, ct);
        if (investor is null || !investor.IsActiveVerified) return Result.Failure(ErrorCodes.InvestorNotVerified, "Documents can only be shared with verified investors.");
        if (await documents.GetActiveGrantAsync(documentId, investorId, ct) is null)
        {
            await documents.AddGrantAsync(new Entities.Startups.DocumentAccessGrant { DocumentId = documentId, InvestorId = investorId, GrantedAtUtc = clock.UtcNow }, ct);
            await activity.NotifyAccountAsync(investor.AccountId, "A startup shared a confidential document with you.", "/library", ct);
        }
        await uow.SaveChangesAsync(ct);
        return Result.Success("Access granted to this investor only.");
    }

    public async Task<Result> RevokeAsync(Actor owner, int documentId, int investorId, CancellationToken ct = default)
    {
        var d = await documents.GetByIdAsync(documentId, ct);
        if (d is null || !owner.IsFounder || owner.StartupId != d.StartupId) return Result.Failure(Guard.Forbidden());
        if (await documents.GetActiveGrantAsync(documentId, investorId, ct) is { } grant) grant.RevokedAtUtc = clock.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Access revoked immediately.");
    }

    public async Task<Result<(string StorageKey, string FileName)>> AuthorizeDownloadAsync(Actor actor, int documentId, CancellationToken ct = default)
    {
        var d = await documents.GetByIdAsync(documentId, ct);
        if (d is null) return Guard.NotFound("Document");
        if (d.ScanStatus != ScanStatus.Clean) return new Error(ErrorCodes.UploadPendingScan, "This file is still being scanned.");
        var allowed = (actor.IsFounder && actor.StartupId == d.StartupId) ||
                      (actor.IsInvestor && await documents.GetActiveGrantAsync(d.Id, actor.InvestorId!.Value, ct) is not null &&
                       (await investors.GetByIdAsync(actor.InvestorId!.Value, ct))?.IsActiveVerified == true);
        if (!allowed) return new Error(ErrorCodes.ForbiddenDocument, "You do not have access to this document.");
        await activity.AuditAsync(actor, "Confidential document download", $"document:{d.Id}", "authorised access", ct: ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success((d.StorageKey, d.FileName));
    }
}
