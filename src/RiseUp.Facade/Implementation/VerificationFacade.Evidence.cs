using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Verification;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Verification;

public sealed partial class VerificationFacade
{
    private static bool HasRequiredEvidence(VerificationSubject subject, LicenceHolderType? holder, IEnumerable<string> kinds)
    {
        var documents = kinds.ToList();
        var required = new HashSet<string> { subject == VerificationSubject.Startup ? "TradeLicence" : "Credential" };
        if (subject == VerificationSubject.Startup && holder == LicenceHolderType.Company) required.Add("OwnershipProof");
        required.Add(documents.Contains("Passport") ? "Passport" : "NID");
        return documents.Count == required.Count && required.SetEquals(documents);
    }

    private async Task<bool> IsSubjectActiveAsync(VerificationApplication app, CancellationToken ct) =>
        app.Subject == VerificationSubject.Startup
            ? app.StartupId is int sid && await startups.GetByIdAsync(sid, ct) is { IsDeleted: false }
            : app.InvestorId is int iid && await investors.GetByIdAsync(iid, ct) is { IsDeleted: false };

    public async Task<Result<(Stream Content, string FileName)>> OpenEvidenceAsync(Actor admin, int applicationId, int documentId, CancellationToken ct = default)
    {
        if (!CanReview(admin)) return Guard.Forbidden();
        var app = await applications.GetByIdAsync(applicationId, ct);
        var doc = app?.Documents.SingleOrDefault(d => d.Id == documentId);
        if (app?.Status != VerificationStatus.Pending || doc is null || doc.Erased || doc.Retention == EvidenceRetention.Erasing ||
            !await IsSubjectActiveAsync(app, ct)) return Guard.NotFound("Evidence");
        if (doc.ScanStatus == ScanStatus.Infected) return new Error(ErrorCodes.Validation, "This file is quarantined.");
        await using var source = await storage.OpenAsync(doc.StorageKey, ct);
        if (source is null) return Guard.NotFound("Evidence file");

        // Buffer the exact bytes that are scanned so no file can be substituted before the response.
        var content = new MemoryStream();
        try
        {
            await source.CopyToAsync(content, ct);
            content.Position = 0;
            var verdict = await scanner.ScanAsync(content, ct);
            if (verdict != ScanVerdict.Clean)
            {
                doc.ScanStatus = verdict == ScanVerdict.Infected ? ScanStatus.Infected : ScanStatus.Pending;
                await uow.SaveChangesAsync(ct);
                content.Dispose();
                return new Error(ErrorCodes.UploadPendingScan, "This file is quarantined. A clean scan is required.");
            }
            content.Position = 0;
            var header = new byte[8];
            var length = content.Read(header);
            var extension = header.AsSpan(0, length).StartsWith("%PDF"u8) ? ".pdf" :
                header.AsSpan(0, length).StartsWith(new byte[] { 0xff, 0xd8, 0xff }) ? ".jpg" :
                header.AsSpan(0, length).StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47 }) ? ".png" : null;
            if (extension is null)
            {
                content.Dispose();
                return new Error(ErrorCodes.Validation, "This evidence must be a PDF, JPEG or PNG.");
            }
            content.Position = 0;
            doc.ScanStatus = ScanStatus.Clean;
            await activity.AuditAsync(admin, "Opened verification evidence", $"verification-document:{doc.Id}", $"application:{app.Id}", true, ct);
            await uow.SaveChangesAsync(ct);
            return Result.Success<(Stream, string)>((content, $"evidence-{doc.Id}{extension}"));
        }
        catch
        {
            content.Dispose();
            throw;
        }
    }

    public async Task<Result> SetEvidenceHoldAsync(Actor admin, int applicationId, int documentId, bool hold, string reason, CancellationToken ct = default)
    {
        if (!admin.IsAdmin(AdminRole.SuperAdmin) || admin.AccountId is null) return Result.Failure(Guard.Forbidden());
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 500)
            return Result.Failure(ErrorCodes.Validation, "Enter a hold or release reason of 5 to 500 characters.");
        var app = await applications.GetByIdAsync(applicationId, ct);
        var doc = app?.Documents.SingleOrDefault(d => d.Id == documentId);
        if (doc is null || doc.Erased || doc.Retention == EvidenceRetention.Erasing) return Result.Failure(Guard.NotFound("Retainable evidence"));
        doc.Retention = hold ? EvidenceRetention.Held : EvidenceRetention.Available;
        await activity.AuditAsync(admin, hold ? "Placed evidence hold" : "Released evidence hold", $"verification-document:{doc.Id}", reason.Trim(), ct: ct);
        try { await uow.SaveChangesAsync(ct); }
        catch (PersistenceConflictException) { return Result.Failure(ErrorCodes.Conflict, "Evidence state changed. Refresh the queue."); }
        return Result.Success(hold ? "Evidence held." : "Hold released.");
    }

    public async Task ProcessEvidenceErasureAsync(CancellationToken ct = default)
    {
        // A process can stop between staging bytes and committing an application. Keep referenced files.
        foreach (var key in intake.ListStaleReceipts(clock.UtcNow.AddHours(-1)))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (!await applications.HasDocumentKeyAsync(key, ct)) await storage.DeleteAsync(key, ct);
                intake.Complete(key);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning("An abandoned verification upload could not be reconciled. It will be retried.");
            }
        }
        var afterId = 0;
        while (true)
        {
            var batch = await applications.ListForErasureAsync(afterId, ct);
            if (batch.Count == 0) return;
            foreach (var doc in batch)
            {
                afterId = doc.Id;
                if (doc.Retention != EvidenceRetention.Erasing)
                {
                    // Commit the claim before touching storage. A concurrent hold conflicts with this write.
                    doc.Retention = EvidenceRetention.Erasing;
                    await uow.SaveChangesAsync(ct);
                }
                try { await storage.DeleteAsync(doc.StorageKey, ct); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning("Erasure of verification document {Id} will be retried.", doc.Id);
                    continue;
                }
                doc.Erased = true;
                doc.StorageKey = "erased";
                await activity.AuditAsync(Actor.System, "Verification evidence erased", $"verification-document:{doc.Id}", "account deleted", ct: ct);
                await uow.SaveChangesAsync(ct);
            }
        }
    }

    public async Task ExpireDueAsync(CancellationToken ct = default)
    {
        foreach (var subject in Enum.GetValues<VerificationSubject>())
            foreach (var id in await applications.ListExpiredSubjectIdsAsync(subject, clock.UtcNow, ct))
                await ExpireAsync(Actor.System, subject, id, "verification validity ended", ct);
    }
}
