using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.ConnectionRequests;

public interface IConnectionRequestFacade
{
    Task<Result<int>> SendPitchAsync(Actor actor, SendPitchRequest request, CancellationToken ct = default);
    Task<Result<int>> SaveDraftAsync(Actor actor, int? investorId, string text, int? draftId, CancellationToken ct = default);
    Task<IReadOnlyList<DraftDto>> ListDraftsAsync(Actor actor, CancellationToken ct = default);
    Task<Result<int>> SendInvitationAsync(Actor actor, int startupId, string? note, string idempotencyKey, CancellationToken ct = default);
    Task<Result> RespondAsync(Actor actor, int requestId, bool accept, string? note, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectedStartupDto>> ListConnectedStartupsAsync(Actor actor, CancellationToken ct = default);
    Task<IReadOnlyList<RequestDto>> ListForActorAsync(Actor actor, CancellationToken ct = default);
    Task<int> InvitationsUsedTodayAsync(int investorId, CancellationToken ct = default);
    /// <summary>Background job: unanswered requests expire exactly 14×24h after the original send (FR-074/085).</summary>
    Task<int> ExpireDueAsync(CancellationToken ct = default);
}
