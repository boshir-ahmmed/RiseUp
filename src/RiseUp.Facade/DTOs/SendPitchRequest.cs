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

public sealed record SendPitchRequest(int InvestorId, string Text, string IdempotencyKey, int? DraftId = null, IReadOnlyList<int>? DocumentIds = null);
