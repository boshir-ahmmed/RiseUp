using RiseUp.Entities.Common;

namespace RiseUp.Entities.Payments;

public class TokenPackage : BaseEntity
{
    public int Tokens { get; set; }
    public decimal PriceBdt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PaymentOrder : BaseEntity, IConcurrencyAware
{
    public string OrderNumber { get; set; } = string.Empty;
    public int StartupId { get; set; }
    public int PackageId { get; set; }
    public int TokensSnapshot { get; set; }
    public decimal PriceBdtSnapshot { get; set; }
    public string Currency { get; set; } = "BDT";
    public PaymentStatus Status { get; set; }
    /// <summary>Unique when present: one provider transaction credits at most once (FR-129).</summary>
    public string? ProviderTransactionId { get; set; }
    public DateTime? CreditedAtUtc { get; set; }
    public string? Method { get; set; }
    public Guid Version { get; set; }
}

public class PaymentProviderEvent : BaseEntity
{
    public int? OrderId { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public bool SignatureValid { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}

public class PromotionPackage : BaseEntity
{
    public int Days { get; set; }
    public int CostPurchasedTokens { get; set; }
}
