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

public sealed record RequestDto(int Id, RequestType Type, int StartupId, string StartupName, int InvestorId, string InvestorName, string Message,
    DateTime SentAtUtc, DateTime ExpiresAtUtc, RequestStatus Status, string? DecisionNote, TokenBucket? Bucket, DateTime? ResendAllowedAtUtc, bool InfoRequested, bool NeedsFounderReply);
