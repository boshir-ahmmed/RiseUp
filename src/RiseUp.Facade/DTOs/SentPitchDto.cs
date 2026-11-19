using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record SentPitchDto(int RequestId, int InvestorId, string InvestorName, DateTime SentAtUtc, DateTime ExpiresAtUtc, RequestStatus Status, int? PackageVersion,
    int AttachedDocuments, RoomStatsDto Stats, int Questions, int Unanswered, bool InfoRequested);
