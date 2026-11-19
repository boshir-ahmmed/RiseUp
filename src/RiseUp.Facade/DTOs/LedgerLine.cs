using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Wallets;

public sealed record LedgerLine(int Id, DateTime AtUtc, string Description, TokenBucket Bucket, int Amount, string Reference);
