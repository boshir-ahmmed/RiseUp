using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.AI;
using RiseUp.DataAccess;
using RiseUp.DataAccess.Services;
using RiseUp.Entities.Common;
using RiseUp.Entities.Payments;
using RiseUp.Facade;
using RiseUp.Facade.Common;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Identity;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.TeamAccounts;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RISEUP_TEST_SQLSERVER")))
            Skip = "Requires a disposable SQL Server (configured in CI).";
    }
}

public class SqlServerCommerceTests
{
    [SqlServerFact]
    public async Task MigratedSqlServerRegistersCreditsAndSpendsExactlyOnce()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RISEUP_TEST_SQLSERVER"))
        {
            InitialCatalog = "RiseUpTest_" + Guid.NewGuid().ToString("N")
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:RiseUp"] = connection.ConnectionString, ["Payments:Gateway"] = "Sandbox",
            ["Payments:WebhookSecret"] = "test-secret"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataAccess(config, false);
        services.AddAi(config);
        services.AddFacade();
        await using var provider = services.BuildServiceProvider();
        using var setup = provider.CreateScope();
        var db = setup.ServiceProvider.GetRequiredService<RiseUpDbContext>();
        await db.Database.MigrateAsync("20261121060046_MemberProfileLogo");
        try
        {
            async Task<T> Run<F, T>(Func<F, Task<T>> act) where F : notnull
            {
                using var scope = provider.CreateScope();
                return await act(scope.ServiceProvider.GetRequiredService<F>());
            }
            async Task<int> Register(PrimaryRole role, string email)
            {
                var result = await Run<IAccountFacade, Result<RegisteredAccount>>(f => f.RegisterAsync(new(role, "Test User",
                    email, role == PrimaryRole.Founder ? "01712345678" : "01712345679", "TestPassword123", true, "Test Startup", null)));
                Assert.True(result.IsSuccess, result.Error.Message);
                Assert.True((await Run<IAccountFacade, Result>(f => f.VerifyEmailAsync(result.Value!.EmailVerificationToken))).IsSuccess);
                Assert.True(await db.MemberProfiles.AnyAsync(p => p.AccountId == result.Value!.AccountId));
                return result.Value!.AccountId;
            }
            var owner = await Register(PrimaryRole.Founder, "founder@integration.example");
            await Register(PrimaryRole.Investor, "investor@integration.example");
            // Upgrade a populated installation. Duplicates must be reported without altering either account.
            await db.UserAccounts.Where(u => u.Id != owner).ExecuteUpdateAsync(s => s.SetProperty(u => u.Phone, "+880 1712-345678"));
            var collision = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync());
            Assert.Equal(51002, collision.Number);
            Assert.Equal(2, await db.UserAccounts.CountAsync());
            await db.UserAccounts.Where(u => u.Id != owner).ExecuteUpdateAsync(s => s.SetProperty(u => u.Phone, "01712345679"));
            await db.Database.MigrateAsync("20261124160453_UniqueAccountContacts");
            var startup = await db.Startups.SingleAsync();
            var investor = await db.InvestorProfiles.SingleAsync();
            startup.VerificationStatus = investor.VerificationStatus = VerificationStatus.Verified;
            startup.VerifiedUntilUtc = investor.VerifiedUntilUtc = DateTime.UtcNow.AddDays(30);
            db.TokenPackages.Add(new TokenPackage { Tokens = 10, PriceBdt = 500 });
            await db.SaveChangesAsync();
            var actor = new Actor(owner, "Test User", PrimaryRole.Founder, StartupId: startup.Id);
            Task<Result<RegisteredAccount>> RegisterInvestor(string email, string phone) =>
                Run<IAccountFacade, Result<RegisteredAccount>>(f => f.RegisterAsync(
                    new(PrimaryRole.Investor, "Concurrent Investor", email, phone, "TestPassword123", true)));
            var duplicate = await RegisterInvestor("duplicate@integration.example", "+880 1712-345678");
            Assert.Equal(ErrorCodes.PhoneTaken, duplicate.Error.Code);
            var attempts = await Task.WhenAll(
                RegisterInvestor("first@integration.example", "01612345678"),
                RegisterInvestor("second@integration.example", "008801612345678"));
            Assert.Single(attempts, result => result.IsSuccess);
            Assert.Single(attempts, result => result.IsFailure);
            var order = await Run<IPaymentFacade, Result<CheckoutDto>>(f => f.CreateOrderAsync(actor, 1));
            Assert.True(order.IsSuccess, order.Error.Message);
            var payload = $"{order.Value!.OrderNumber}|test-bank-1|PAID";
            Task<Result> Confirm() => Run<IPaymentFacade, Result>(f => f.HandleWebhookAsync(payload, SandboxPaymentGateway.Sign(payload, "test-secret")));
            var callbacks = await Task.WhenAll(Confirm(), Confirm());
            Assert.Contains(callbacks, r => r.IsSuccess);
            Assert.True((await Confirm()).IsFailure);
            var request = new SendPitchRequest(investor.Id, "We make crop storage equipment for growers and would like to discuss a seed investment.", "one-pitch");
            Assert.True((await Run<IConnectionRequestFacade, Result<int>>(f => f.SendPitchAsync(actor, request))).IsSuccess);
            Assert.True((await Run<IConnectionRequestFacade, Result<int>>(f => f.SendPitchAsync(actor, request))).IsFailure);
            Assert.Equal(9, (await db.Wallets.AsNoTracking().SingleAsync()).PurchasedBalance);
            Assert.Equal(2, await db.TokenLedger.CountAsync());

            // Upgrade a legacy pitch, then race two replies to the imported item.
            var sent = await db.ConnectionRequests.SingleAsync();
            sent.InfoRequestMessage = "Quarterly revenue";
            sent.InfoRequestedAtUtc = sent.SentAtUtc;
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO TeamAccounts (StartupId, Username, Email, FullName, Designation, PasswordHash,
                    Status, PreviousConversationIds, CreatedAtUtc)
                VALUES ({startup.Id}, 'legacy.member', 'legacy@integration.example', 'Legacy Member', 'Analyst',
                    'test-hash', 2, '17,24', SYSUTCDATETIME())
                """);
            await db.Database.MigrateAsync();
            var legacyMember = await db.TeamAccounts.AsNoTracking().SingleAsync();
            Assert.Equal(TeamGroupMode.Selected, legacyMember.GroupMode);
            Assert.Equal("17,24", legacyMember.PreviousConversationIds);
            var teamMember = await Run<ITeamAccountFacade, Result<OneTimeCredential>>(f => f.CreateAsync(actor,
                new("Team Analyst", "Analyst", "team.analyst", "Analyst@integration.example", null, "AnalystPass123")));
            Assert.True(teamMember.IsSuccess, teamMember.Error.Message);
            var login = await Run<IAccountFacade, Result<SignedInUser>>(f => f.SignInAsync(" ANALYST@INTEGRATION.EXAMPLE ", "AnalystPass123"));
            Assert.True(login.IsSuccess, login.Error.Message);
            Assert.Equal(teamMember.Value!.TeamAccountId, login.Value!.TeamAccountId);
            var item = await db.PitchInfoItems.AsNoTracking().SingleAsync();
            Assert.Equal(sent.InfoRequestMessage, item.Prompt);
            Assert.Equal(sent.InfoRequestedAtUtc, item.CreatedAtUtc);
            Task<Result> Reply(string text) => Run<IPitchRoomFacade, Result>(f => f.ReplyToInfoAsync(actor, sent.Id, item.Id, text, null));
            var replies = await Task.WhenAll(Reply("First response"), Reply("Second response"));
            Assert.Single(replies, r => r.IsSuccess);
            Assert.Single(replies, r => r.Error.Code == ErrorCodes.Conflict);
            Assert.Equal(1, await db.Notifications.CountAsync(n => n.Text.Contains("replied to your information request")));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
