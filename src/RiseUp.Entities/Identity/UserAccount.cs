using RiseUp.Entities.Common;

namespace RiseUp.Entities.Identity;

/// <summary>Founder, Investor or Administrator login. The primary role is immutable (FR-002).</summary>
public class UserAccount : BaseEntity, IConcurrencyAware
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public PrimaryRole PrimaryRole { get; set; }
    public AdminRole AdminRole { get; set; }
    public bool EmailVerified { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public string AcceptedTermsVersion { get; set; } = string.Empty;
    public string AcceptedPrivacyVersion { get; set; } = string.Empty;
    public DateTime? PolicyAcceptedAtUtc { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid Version { get; set; }
}

/// <summary>Single-use, hashed token for email verification or password reset.</summary>
public class AccountToken : BaseEntity
{
    public int UserAccountId { get; set; }
    public string Purpose { get; set; } = string.Empty; // "verify-email" | "reset-password"
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
