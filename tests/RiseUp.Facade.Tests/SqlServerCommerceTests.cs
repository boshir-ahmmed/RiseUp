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
        await db.Database.MigrateAsync();
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
                    email, "01712345678", "TestPassword123", true, "Test Startup", null)));
                Assert.True(result.IsSuccess, result.Error.Message);
                Assert.True((await Run<IAccountFacade, Result>(f => f.VerifyEmailAsync(result.Value!.EmailVerificationToken))).IsSuccess);
                Assert.True(await db.MemberProfiles.AnyAsync(p => p.AccountId == result.Value!.AccountId));
                return result.Value!.AccountId;
            }
            var owner = await Register(PrimaryRole.Founder, "founder@integration.example");
            await Register(PrimaryRole.Investor, "investor@integration.example");
            var startup = await db.Startups.SingleAsync();
            var investor = await db.InvestorProfiles.SingleAsync();
            startup.VerificationStatus = investor.VerificationStatus = VerificationStatus.Verified;
            startup.VerifiedUntilUtc = investor.VerifiedUntilUtc = DateTime.UtcNow.AddDays(30);
            db.TokenPackages.Add(new TokenPackage { Tokens = 10, PriceBdt = 500 });
            await db.SaveChangesAsync();
            var actor = new Actor(owner, "Test User", PrimaryRole.Founder, StartupId: startup.Id);
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
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
