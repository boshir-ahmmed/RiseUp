using RiseUp.Entities.Common;

namespace RiseUp.Entities.Wallets;

/// <summary>Two separate non-negative balances, enforced by CHECK constraints and concurrency token (FR-124).</summary>
public class StartupWallet : BaseEntity, IConcurrencyAware
{
    public int StartupId { get; set; }
    public int WelcomeBalance { get; set; }
    public int PurchasedBalance { get; set; }
    public Guid Version { get; set; }
}

/// <summary>Append-only. Corrections are new compensating rows; existing rows are never edited.</summary>
public class TokenLedgerEntry : BaseEntity
{
    public int StartupId { get; set; }
    public string Description { get; set; } = string.Empty;
    public TokenBucket Bucket { get; set; }
    public int Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    /// <summary>Unique: the same operation can never post twice (e.g. welcome grant, webhook credit).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    public int? ActorAccountId { get; set; }
}
