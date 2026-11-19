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

public sealed record SubmitVerificationRequest(LicenceHolderType? LicenceHolderType, string? LicenceHolderName, IReadOnlyList<(string Kind, string StorageKey)> Documents);
