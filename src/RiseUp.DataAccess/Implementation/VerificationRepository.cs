using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Governance;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;

namespace RiseUp.DataAccess.Repositories;

public sealed class VerificationRepository(RiseUpDbContext db) : Repository<VerificationApplication>(db), IVerificationRepository
{
    public override Task<VerificationApplication?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Set.Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == id, ct);
    public async Task<IReadOnlyList<VerificationApplication>> ListAsync(VerificationStatus? status, CancellationToken ct = default) =>
        await Set.Include(a => a.Documents).AsNoTracking().Where(a => status == null || a.Status == status)
            .Where(a => a.Status == VerificationStatus.Pending || a.Documents.Any(d => d.Retention == EvidenceRetention.Held) ||
                Set.OrderByDescending(recent => recent.SubmittedAtUtc).ThenByDescending(recent => recent.Id).Take(200).Select(recent => recent.Id).Contains(a.Id))
            .OrderByDescending(a => a.SubmittedAtUtc).ThenByDescending(a => a.Id).ToListAsync(ct);
    public Task<VerificationApplication?> GetLatestForAsync(VerificationSubject subject, int subjectId, CancellationToken ct) =>
        Set.AsNoTracking().Include(a => a.Documents)
            .Where(a => a.Subject == subject && (subject == VerificationSubject.Startup ? a.StartupId == subjectId : a.InvestorId == subjectId))
            .OrderByDescending(a => a.Status == VerificationStatus.Pending).ThenByDescending(a => a.SubmittedAtUtc).ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(ct);

    public Task<VerificationApplication?> GetPendingForAsync(VerificationSubject subject, int subjectId, CancellationToken ct = default) =>
        Set.Include(a => a.Documents).FirstOrDefaultAsync(a => a.Subject == subject && a.Status == VerificationStatus.Pending &&
            (subject == VerificationSubject.Startup ? a.StartupId == subjectId : a.InvestorId == subjectId), ct);
    public async Task<IReadOnlyList<VerificationDocument>> ListForErasureAsync(int afterId, CancellationToken ct) =>
        await Set.Where(a => a.Subject == VerificationSubject.Startup
                ? Db.Startups.Any(s => s.Id == a.StartupId && s.IsDeleted)
                : Db.InvestorProfiles.Any(i => i.Id == a.InvestorId && i.IsDeleted))
            .SelectMany(a => a.Documents)
            .Where(d => d.Id > afterId && !d.Erased && d.Retention != EvidenceRetention.Held)
            .OrderBy(d => d.Id).Take(50).ToListAsync(ct);

    public Task<bool> HasDocumentKeyAsync(string key, CancellationToken ct) =>
        Db.VerificationDocuments.AnyAsync(d => d.StorageKey == key, ct);

    public Task<bool> WasOpenedByAsync(int documentId, int reviewerId, CancellationToken ct) =>
        Db.AdminAudit.AnyAsync(a => a.ActorAccountId == reviewerId && a.Action == "Opened verification evidence" &&
            a.Target == $"verification-document:{documentId}" && a.IsRestrictedContentAccess, ct);

    public async Task<IReadOnlyList<int>> ListExpiredSubjectIdsAsync(VerificationSubject subject, DateTime now, CancellationToken ct) =>
        subject == VerificationSubject.Startup
            ? await Db.Startups.Where(s => !s.IsDeleted && s.VerificationStatus == VerificationStatus.Verified &&
                (s.VerifiedUntilUtc == null || s.VerifiedUntilUtc <= now)).OrderBy(s => s.Id).Take(100).Select(s => s.Id).ToListAsync(ct)
            : await Db.InvestorProfiles.Where(i => !i.IsDeleted && i.VerificationStatus == VerificationStatus.Verified &&
                (i.VerifiedUntilUtc == null || i.VerifiedUntilUtc <= now)).OrderBy(i => i.Id).Take(100).Select(i => i.Id).ToListAsync(ct);

    public Task<OwnershipTransferCase?> GetTransferAsync(int id, CancellationToken ct = default) => Db.OwnershipTransferCases.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<OwnershipTransferCase?> GetOpenTransferForStartupAsync(int startupId, CancellationToken ct = default) =>
        Db.OwnershipTransferCases.FirstOrDefaultAsync(t => t.StartupId == startupId && (t.Status == TransferStatus.AwaitingIncomingConsent || t.Status == TransferStatus.AwaitingReview), ct);
    public async Task AddTransferAsync(OwnershipTransferCase transfer, CancellationToken ct = default) => await Db.OwnershipTransferCases.AddAsync(transfer, ct);
    public async Task<IReadOnlyList<OwnershipTransferCase>> ListTransfersAsync(CancellationToken ct = default) =>
        await Db.OwnershipTransferCases.AsNoTracking().OrderByDescending(t => t.CreatedAtUtc).Take(100).ToListAsync(ct);
}
