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

public interface IVerificationRepository : IRepository<VerificationApplication>
{
    Task<IReadOnlyList<VerificationApplication>> ListAsync(VerificationStatus? status, CancellationToken ct = default);
    Task<VerificationApplication?> GetPendingForAsync(VerificationSubject subject, int subjectId, CancellationToken ct = default);
    Task<OwnershipTransferCase?> GetTransferAsync(int id, CancellationToken ct = default);
    Task<OwnershipTransferCase?> GetOpenTransferForStartupAsync(int startupId, CancellationToken ct = default);
    Task AddTransferAsync(OwnershipTransferCase transfer, CancellationToken ct = default);
    Task<IReadOnlyList<OwnershipTransferCase>> ListTransfersAsync(CancellationToken ct = default);
}
