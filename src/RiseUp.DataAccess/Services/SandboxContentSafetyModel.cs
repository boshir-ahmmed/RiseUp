using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

/// <summary>
/// DEMO/SANDBOX AI Model 1. Returns an explicit validated Approve/Reject decision for posts and pitches.
/// Replace with a server-side Gemini adapter that validates structured output; never trust free-form prose.
/// </summary>
public sealed partial class SandboxContentSafetyModel(IOptions<AiOptions> options) : IContentSafetyModel
{
    public const string Version = "m1-sandbox-2026.11";

    [GeneratedRegex(@"guarantee[ds]?\s+(return|profit)|risk[- ]?free|double your money|no risk|100%\s*return|\bscam\b|whatsapp me", RegexOptions.IgnoreCase)]
    private static partial Regex Misleading();

    public Task<ContentSafetyResult> CheckAsync(string purpose, string text, CancellationToken ct = default)
    {
        if (options.Value.SimulateModel1Outage)
            return Task.FromResult(new ContentSafetyResult(SafetyOutcome.Unavailable, null, Version));
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
            return Task.FromResult(new ContentSafetyResult(SafetyOutcome.Reject, "insufficient_content", Version));
        if (Misleading().IsMatch(text))
            return Task.FromResult(new ContentSafetyResult(SafetyOutcome.Reject, "misleading_financial_claim", Version));
        return Task.FromResult(new ContentSafetyResult(SafetyOutcome.Approve, null, Version));
    }
}
