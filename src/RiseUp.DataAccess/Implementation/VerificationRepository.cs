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
        await Set.Include(a => a.Documents).AsNoTracking().Where(a => status == null || a.Status == status).OrderByDescending(a => a.SubmittedAtUtc).Take(200).ToListAsync(ct);
    public Task<VerificationApplication?> GetPendingForAsync(VerificationSubject subject, int subjectId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(a => a.Subject == subject && a.Status == VerificationStatus.Pending &&
            (subject == VerificationSubject.Startup ? a.StartupId == subjectId : a.InvestorId == subjectId), ct);
    public Task<OwnershipTransferCase?> GetTransferAsync(int id, CancellationToken ct = default) => Db.OwnershipTransferCases.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<OwnershipTransferCase?> GetOpenTransferForStartupAsync(int startupId, CancellationToken ct = default) =>
        Db.OwnershipTransferCases.FirstOrDefaultAsync(t => t.StartupId == startupId && (t.Status == TransferStatus.AwaitingIncomingConsent || t.Status == TransferStatus.AwaitingReview), ct);
    public async Task AddTransferAsync(OwnershipTransferCase transfer, CancellationToken ct = default) => await Db.OwnershipTransferCases.AddAsync(transfer, ct);
    public async Task<IReadOnlyList<OwnershipTransferCase>> ListTransfersAsync(CancellationToken ct = default) =>
        await Db.OwnershipTransferCases.AsNoTracking().OrderByDescending(t => t.CreatedAtUtc).Take(100).ToListAsync(ct);
}
