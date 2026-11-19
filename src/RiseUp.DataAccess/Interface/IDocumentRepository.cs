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

namespace RiseUp.Entities.Abstractions;

public interface IDocumentRepository : IRepository<PrivateDocument>
{
    Task<IReadOnlyList<PrivateDocument>> ListForStartupAsync(int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<PrivateDocument>> ListPendingScanAsync(int take, CancellationToken ct = default);
    Task<DocumentAccessGrant?> GetActiveGrantAsync(int documentId, int investorId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentAccessGrant>> ListActiveGrantsForInvestorAsync(int investorId, CancellationToken ct = default);
    Task AddGrantAsync(DocumentAccessGrant grant, CancellationToken ct = default);
}
