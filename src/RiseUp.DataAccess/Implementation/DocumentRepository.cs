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

public sealed class DocumentRepository(RiseUpDbContext db) : Repository<PrivateDocument>(db), IDocumentRepository
{
    public async Task<IReadOnlyList<PrivateDocument>> ListForStartupAsync(int startupId, CancellationToken ct = default) =>
        await Set.Where(d => d.StartupId == startupId).OrderBy(d => d.Id).ToListAsync(ct);
    public async Task<IReadOnlyList<PrivateDocument>> ListPendingScanAsync(int take, CancellationToken ct = default) =>
        await Set.Where(d => d.ScanStatus == ScanStatus.Pending).OrderBy(d => d.Id).Take(take).ToListAsync(ct);
    public Task<DocumentAccessGrant?> GetActiveGrantAsync(int documentId, int investorId, CancellationToken ct = default) =>
        Db.DocumentAccessGrants.FirstOrDefaultAsync(g => g.DocumentId == documentId && g.InvestorId == investorId && g.RevokedAtUtc == null, ct);
    public async Task<IReadOnlyList<DocumentAccessGrant>> ListActiveGrantsForInvestorAsync(int investorId, CancellationToken ct = default) =>
        await Db.DocumentAccessGrants.AsNoTracking().Where(g => g.InvestorId == investorId && g.RevokedAtUtc == null).ToListAsync(ct);
    public async Task AddGrantAsync(DocumentAccessGrant grant, CancellationToken ct = default) => await Db.DocumentAccessGrants.AddAsync(grant, ct);
}
