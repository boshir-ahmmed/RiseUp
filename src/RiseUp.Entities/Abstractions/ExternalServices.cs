using RiseUp.Entities.Common;

namespace RiseUp.Entities.Abstractions;

/// <summary>Password hashing port (implemented with ASP.NET Core PasswordHasher in DataAccess).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public enum SafetyOutcome { Approve, Reject, Unavailable }

public sealed record ContentSafetyResult(SafetyOutcome Outcome, string? ReasonCode, string ModelVersion);

/// <summary>
/// AI Model 1 — approves or rejects ONLY startup posts and founder pitches (FR-066, FR-110).
/// It never writes or rewrites text and is never called for comments, replies or private chats.
/// </summary>
public interface IContentSafetyModel
{
    Task<ContentSafetyResult> CheckAsync(string purpose, string text, CancellationToken ct = default);
}

/// <summary>Only fields the requesting user is already allowed to see are ever placed in a profile.</summary>
public sealed record MatchProfile(int Id, IReadOnlyList<string> Industries, IReadOnlyList<FundingStage> Stages,
    decimal? MinUsd, decimal? MaxUsd, string Location, string? PublicText);

public sealed record MatchScore(int CandidateId, int Score, IReadOnlyList<string> Reasons);

/// <summary>AI Model 2 — two-sided relevance ranking and promotion targeting (FR-057, FR-138).</summary>
public interface IAiMatchingService
{
    Task<IReadOnlyList<MatchScore>> RankAsync(MatchProfile subject, IReadOnlyList<MatchProfile> candidates, CancellationToken ct = default);
}

public sealed record CheckoutSession(string RedirectUrl, string GatewayName, bool IsSandbox);

/// <summary>Bangladesh payment gateway port. Browser redirects never count as proof of payment (FR-129).</summary>
public interface IPaymentGateway
{
    string Name { get; }
    bool IsSandbox { get; }
    Task<CheckoutSession> CreateCheckoutAsync(string orderNumber, decimal amountBdt, CancellationToken ct = default);
    bool VerifySignature(string payload, string signature);
    /// <summary>Sandbox only: signs a simulated confirmation. Real gateways return null.</summary>
    string? CreateSandboxSignature(string payload);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default);
    Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

/// <summary>Second-factor verifier (TOTP in production). Administrators must always pass it (FR-009).</summary>
public interface IMfaVerifier
{
    bool Verify(string? secret, string code);
}

public enum ScanVerdict { Clean, Infected, Unavailable }

/// <summary>Malware scanner port (SR-13). On outage files stay quarantined — never released unscanned.</summary>
public interface IMalwareScanner
{
    Task<ScanVerdict> ScanAsync(Stream content, CancellationToken ct = default);
}
