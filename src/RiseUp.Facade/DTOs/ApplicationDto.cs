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

public sealed record ApplicationDto(int Id, VerificationSubject Subject, int SubjectId, string SubjectName, string? OwnerName, VerificationStatus Status,
    LicenceHolderType? LicenceHolderType, string? LicenceHolderName, DateTime SubmittedAtUtc, string? DecisionReason, IReadOnlyList<string> DocumentKinds);
