using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Verification;

public interface IVerificationFacade
{
    Task<Result<int>> SubmitAsync(Actor actor, SubmitVerificationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDto>> ListQueueAsync(Actor admin, CancellationToken ct = default);
    Task<Result<(Stream Content, string FileName)>> OpenEvidenceAsync(Actor admin, int applicationId, int documentId, CancellationToken ct = default);
    Task<Result> SetEvidenceHoldAsync(Actor admin, int applicationId, int documentId, bool hold, string reason, CancellationToken ct = default);
    Task ProcessEvidenceErasureAsync(CancellationToken ct = default);
    Task ExpireDueAsync(CancellationToken ct = default);
    Task<Result> ApproveAsync(Actor admin, int applicationId, string note, DateOnly? earliestEvidenceExpiry = null, CancellationToken ct = default);
    Task<Result> RejectAsync(Actor admin, int applicationId, string reason, CancellationToken ct = default);
    Task<Result> ExpireAsync(Actor admin, VerificationSubject subject, int subjectId, string reason, CancellationToken ct = default);
    Task<Result<int>> RequestOwnershipTransferAsync(Actor owner, string incomingName, string incomingEmail, CancellationToken ct = default);
    Task<Result> RecordIncomingConsentAsync(int transferId, string incomingEmail, CancellationToken ct = default);
    Task<Result> DecideTransferAsync(Actor admin, int transferId, bool approve, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<TransferDto>> ListTransfersAsync(Actor admin, CancellationToken ct = default);
}
