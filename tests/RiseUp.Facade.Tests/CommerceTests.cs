using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.DataAccess.Services;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Identity;
using RiseUp.Facade.Notifications;
using RiseUp.Facade.Payments;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Tests;

public class CommerceTests
{
    private static readonly BillingAddress Billing = new("Example Road", "Dhaka", "1205", "Bangladesh", "01712345678");
    private sealed class Handler(Func<HttpRequestMessage, Task<object>> reply) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(await reply(request)), Encoding.UTF8, "application/json") };
    }

    [Theory]
    [InlineData("VALID", "0", "500.00", "BDT", true)]
    [InlineData("VALIDATED", "0", "500", "BDT", true)]
    [InlineData("VALID", "1", "500", "BDT", false)]
    [InlineData("VALID", "0", "499.99", "BDT", false)]
    [InlineData("VALID", "0", "500", "USD", false)]
    [InlineData("PENDING", "0", "500", "BDT", false)]
    public async Task HostedCheckoutUsesAuthenticatedProviderEvidence(string status, string risk, string amount, string currency, bool shouldCredit)
    {
        string? order = null;
        using var http = new HttpClient(new Handler(async r =>
        {
            Assert.Equal("sandbox.sslcommerz.com", r.RequestUri!.Host);
            if (r.Method == HttpMethod.Post)
            {
                var fields = QueryHelpers.ParseQuery(await r.Content!.ReadAsStringAsync());
                order = fields["tran_id"].ToString();
                Assert.Equal("500.00", fields["total_amount"]);
                Assert.Equal("https://riseup.example/payments/sslcommerz/ipn", fields["ipn_url"]);
                Assert.Equal("BDT", fields["currency"]);
                Assert.False(fields.ContainsKey("multi_card_name"));
                return new { status = "SUCCESS", GatewayPageURL = "https://sandbox.sslcommerz.com/pay" };
            }
            var query = QueryHelpers.ParseQuery(r.RequestUri.Query);
            Assert.Equal("unit-test", query["store_passwd"]);
            var row = new { tran_id = order, val_id = "verified-id", bank_tran_id = "bank-reference", status, risk_level = risk, amount, currency, card_type = "NAGAD" };
            if (query.ContainsKey("tran_id")) return new { APIConnect = "DONE", no_of_trans_found = 1, element = new[] { row } };
            return row;
        }));
        var adapter = new SslCommerzGateway(new() { StoreId = "test", StorePassword = "unit-test", PublicOrigin = "https://riseup.example" }, http);
        await using var h = await TestHost.CreateAsync(configure: services => services.AddSingleton<IPaymentGateway>(adapter));
        var founder = await h.Founder();
        var before = await h.Db(db => db.Wallets.Where(w => w.StartupId == founder.StartupId).Select(w => w.PurchasedBalance).SingleAsync());
        var session = await h.Run<IPaymentFacade, Result<CheckoutDto>>(f => f.CreateOrderAsync(founder, 1, Billing));
        Assert.True(session.IsSuccess, session.Error.Message);
        Assert.Equal(order, session.Value!.OrderNumber);
        Assert.Equal("https://sandbox.sslcommerz.com/pay", session.Value.RedirectUrl);
        Assert.Equal(before, await h.Db(db => db.Wallets.Where(w => w.StartupId == founder.StartupId).Select(w => w.PurchasedBalance).SingleAsync()));
        Assert.True((await h.Run<IPaymentFacade, Result>(f => f.ConfirmHostedAsync(order!, "different-validation-id"))).IsFailure);
        Assert.Equal(shouldCredit, (await h.Run<IPaymentFacade, Result>(f => f.ConfirmHostedAsync(order!, "verified-id"))).IsSuccess);
        var reconcile = await h.Run<IPaymentFacade, Result>(f => f.ConfirmHostedAsync(order!, null));
        Assert.Equal(shouldCredit, reconcile.IsSuccess);
        Assert.Equal(before + (shouldCredit ? 10 : 0), await h.Db(db => db.Wallets.Where(w => w.StartupId == founder.StartupId).Select(w => w.PurchasedBalance).SingleAsync()));
        Assert.Equal(shouldCredit ? 1 : 0, await h.Db(db => db.TokenLedger.CountAsync(l => l.Reference == "order:" + order)));
    }

    private sealed class Mailbox : IEmailSender
    {
        public bool Offline { get; set; } = true;
        public List<string> Messages { get; } = [];
        public Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
        {
            if (Offline) throw new IOException("Mailbox offline");
            Messages.Add(body);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AccountEmailsRetryAndDeliverWorkingSingleUseLinks()
    {
        var mailbox = new Mailbox();
        await using var h = await TestHost.CreateAsync(configure: services => services.AddSingleton<IEmailSender>(mailbox));
        var email = "owner@signup.example";
        var registration = await h.Run<IAccountFacade, Result<RegisteredAccount>>(f => f.RegisterAsync(
            new(PrimaryRole.Founder, "Test Owner", email, "01712345678", "ComplexPass123", true, "Commerce test", null)));
        Assert.True(registration.IsSuccess, registration.Error.Message);
        async Task Deliver(string type)
        {
            int id = await h.Db(db => db.Outbox.Where(e => e.Type == type && e.ProcessedAtUtc == null).Select(e => e.Id).SingleAsync());
            await h.Run<EmailDelivery, int>(async d => { await d.DeliverAsync(id, new("https://riseup.example"), mailbox, default); return 0; });
        }
        await Assert.ThrowsAsync<IOException>(() => Deliver("email.verify"));
        mailbox.Offline = false;
        h.Clock.Advance(TimeSpan.FromMinutes(6));
        await Deliver("email.verify");
        string Token(string route)
        {
            var link = new Uri(mailbox.Messages.Last().Split('\n').Last());
            Assert.Equal("riseup.example", link.Host);
            Assert.Equal(route, link.AbsolutePath);
            return QueryHelpers.ParseQuery(link.Query)["token"].ToString();
        }
        Assert.True((await h.Run<IAccountFacade, Result>(f => f.VerifyEmailAsync(Token("/account/verify-email")))).IsSuccess);
        await h.Run<IAccountFacade, Result<string>>(f => f.RequestPasswordResetAsync(email));
        await Deliver("email.password-reset");
        var reset = Token("/account/reset-password");
        Assert.True((await h.Run<IAccountFacade, Result>(f => f.ResetPasswordAsync(reset, "UpdatedPass123"))).IsSuccess);
        Assert.True((await h.Run<IAccountFacade, Result>(f => f.ResetPasswordAsync(reset, "AnotherPass123"))).IsFailure);
        Assert.False(await h.Db(db => db.Outbox.AnyAsync(e => e.ProcessedAtUtc != null && e.Payload != "{}")));
    }
}
