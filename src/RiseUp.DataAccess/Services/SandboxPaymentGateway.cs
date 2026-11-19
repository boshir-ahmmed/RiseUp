using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

public sealed class PaymentOptions
{
    public string Gateway { get; set; } = "Sandbox";
    /// <summary>HMAC secret used to verify server-to-server confirmations. Set via secret store, never in source.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}

/// <summary>
/// DEVELOPMENT-ONLY sandbox gateway. DataAccess registration refuses to start in Production with this adapter.
/// A real Bangladesh gateway (merchant onboarding required) implements the same port.
/// </summary>
public sealed class SandboxPaymentGateway(IOptions<PaymentOptions> options) : IPaymentGateway
{
    public string Name => "Sandbox (bKash/Nagad/card simulator)";
    public bool IsSandbox => true;

    public Task<CheckoutSession> CreateCheckoutAsync(string orderNumber, decimal amountBdt, CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession($"/wallet/sandbox-checkout/{orderNumber}", Name, true));

    public bool VerifySignature(string payload, string signature) =>
        !string.IsNullOrEmpty(signature) && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Sign(payload, options.Value.WebhookSecret)), Encoding.ASCII.GetBytes(signature));

    public string? CreateSandboxSignature(string payload) => Sign(payload, options.Value.WebhookSecret);

    public static string Sign(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
