using System.Net;
using System.Net.Mail;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string From { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(options.Username)) client.Credentials = new NetworkCredential(options.Username, options.Password);
        using var message = new MailMessage(options.From, recipient, subject, body);
        message.BodyEncoding = System.Text.Encoding.UTF8;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await client.SendMailAsync(message, timeout.Token);
    }
}
