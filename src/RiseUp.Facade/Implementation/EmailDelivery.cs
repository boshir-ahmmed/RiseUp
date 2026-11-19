using System.Text.Json;
using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Notifications;

public sealed class EmailDelivery(IGovernanceRepository events, IUserAccountRepository accounts, IUnitOfWork uow, IClock clock)
{
    public async Task DeliverAsync(int id, Uri origin, IEmailSender sender, CancellationToken ct)
    {
        var entry = await events.ClaimEmailAsync(id, clock.UtcNow, ct);
        if (entry is null) return;
        using var payload = JsonDocument.Parse(entry.Payload);
        var data = payload.RootElement;
        if (entry.Type == "notification.email")
        {
            var account = await accounts.GetByIdAsync(data.GetProperty("accountId").GetInt32(), ct);
            if (account is { DeletedAtUtc: null, EmailVerified: true })
            {
                var text = data.GetProperty("text").GetString() ?? "";
                await sender.SendAsync(account.Email, text.StartsWith("Receipt ", StringComparison.Ordinal) ? "RiseUp payment receipt" : "RiseUp notification",
                    text + "\n\n" + new Uri(origin, "/dashboard"), ct);
            }
        }
        else
        {
            var verify = entry.Type == "email.verify";
            var token = data.GetProperty("token").GetString()!;
            var record = await accounts.FindTokenAsync(verify ? "verify-email" : "reset-password", SecureTokens.Hash(token), ct);
            if (record != null && record.UsedAtUtc == null && record.ExpiresAtUtc > clock.UtcNow)
            {
                var account = await accounts.GetByIdAsync(record.UserAccountId, ct);
                if (account is { DeletedAtUtc: null } && string.Equals(account.Email, data.GetProperty("Email").GetString(), StringComparison.OrdinalIgnoreCase))
                {
                    var path = verify ? UrlRewrite.Account.VerifyEmail : UrlRewrite.Account.ResetPassword;
                    var link = new Uri(origin, "/" + path + "?token=" + Uri.EscapeDataString(token));
                    await sender.SendAsync(account.Email, verify ? "Verify your RiseUp email" : "Reset your RiseUp password",
                        (verify ? "Verify your email to finish registration." : "Use this single-use link to reset your password.") + "\n\n" + link, ct);
                }
            }
        }
        entry.ProcessedAtUtc = clock.UtcNow;
        entry.Payload = "{}";
        await uow.SaveChangesAsync(ct);
    }
}
