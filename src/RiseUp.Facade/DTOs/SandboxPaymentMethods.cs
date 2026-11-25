namespace RiseUp.Facade.Payments;

public static class SandboxPaymentMethods
{
    public static IReadOnlyList<string> MobileBanking { get; } = ["bKash", "Nagad", "Rocket", "Upay", "Bangla QR"];
    public static IReadOnlyList<string> Cards { get; } = ["Visa", "Mastercard", "American Express"];

    public static bool Supports(string? method) => method != null && (MobileBanking.Contains(method) || Cards.Contains(method));
}
