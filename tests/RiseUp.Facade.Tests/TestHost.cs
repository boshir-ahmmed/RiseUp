using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RiseUp.AI;
using RiseUp.AI.Sandbox;
using RiseUp.DataAccess;
using RiseUp.DataAccess.Seed;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade;
using RiseUp.Facade.Common;
using RiseUp.Facade.Identity;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Tests;

public sealed class TestClock(DateTime start) : IClock
{
    public DateTime UtcNow { get; set; } = start;
    public void Advance(TimeSpan by) => UtcNow += by;
}

/// <summary>Counts Model 1 calls so tests can prove comments/chats never reach AI (acceptance test 25).</summary>
public sealed class SpyContentSafety(IContentSafetyModel inner) : IContentSafetyModel
{
    public int Calls { get; private set; }
    public List<string> Purposes { get; } = [];
    public Task<ContentSafetyResult> CheckAsync(string purpose, string text, CancellationToken ct = default)
    { Calls++; Purposes.Add(purpose); return inner.CheckAsync(purpose, text, ct); }
}

/// <summary>Real Facade + real DataAccess (EF Core) over an isolated in-memory SQLite database with the demo seed.</summary>
public sealed class TestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public ServiceProvider Services { get; }
    public TestClock Clock { get; } = new(new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc)); // 18:00 Asia/Dhaka
    public SpyContentSafety Model1 { get; private set; } = null!;

    private TestHost(Dictionary<string, string?> settings)
    {
        _connection.Open();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:Gateway"] = "Sandbox", ["Payments:WebhookSecret"] = "test-secret", ["Mfa:AllowDemoCodes"] = "true",
            ["Seed:DemoMediaPath"] = DemoMediaPath(), ["Storage:RootPath"] = Path.Combine(Path.GetTempPath(), "riseup-test-storage"),
        }.Where(kv => !settings.ContainsKey(kv.Key)).Concat(settings).ToDictionary(k => k.Key, v => v.Value)).Build();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDataAccess(config, isProduction: false, o => o.UseSqlite(_connection));
        services.AddAi(config);
        services.AddFacade();
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton(sp => Model1 = new SpyContentSafety(
            ActivatorUtilities.CreateInstance<SandboxContentSafetyModel>(sp)));
        services.AddSingleton<IContentSafetyModel>(sp => sp.GetRequiredService<SpyContentSafety>());
        Services = services.BuildServiceProvider();
    }

    /// <summary>Locates src/RiseUp.Web.UI/DemoMedia so the seeded pitch packages include the demo videos.</summary>
    private static string DemoMediaPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RiseUp.slnx"))) dir = dir.Parent;
        return dir is null ? "DemoMedia" : Path.Combine(dir.FullName, "src", "RiseUp.Web.UI", "DemoMedia");
    }

    /// <summary>Stores bytes like the Web layer does and returns the descriptor the Facade expects.</summary>
    public async Task<RiseUp.Facade.Media.UploadedFile> UploadAsync(byte[] bytes, string fileName, string contentType, int? seconds = null)
    {
        using var scope = Services.CreateScope();
        var key = await scope.ServiceProvider.GetRequiredService<IFileStorage>().SaveAsync(new MemoryStream(bytes), fileName);
        return new RiseUp.Facade.Media.UploadedFile(key, fileName, contentType, bytes.Length, seconds);
    }

    public Task<Actor> Team() => SignIn("rafi.ops");

    public static async Task<TestHost> CreateAsync(Dictionary<string, string?>? settings = null)
    {
        var host = new TestHost(settings ?? []);
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RiseUpDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        _ = host.Services.GetRequiredService<SpyContentSafety>();
        return host;
    }

    /// <summary>Runs one "request" in its own DI scope, like ASP.NET Core does.</summary>
    public async Task<T> Run<TFacade, T>(Func<TFacade, Task<T>> action) where TFacade : notnull
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TFacade>());
    }

    public async Task<T> Db<T>(Func<RiseUpDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<RiseUpDbContext>());
    }

    public async Task<Actor> SignIn(string identifier)
    {
        var result = identifier.Contains('@')
            ? await Run<IAccountFacade, RiseUp.Framework.Results.Result<SignedInUser>>(f => f.SignInAsync(identifier, DemoDataSeeder.DemoPassword))
            : await Run<IAccountFacade, RiseUp.Framework.Results.Result<SignedInUser>>(f => f.SignInTeamMemberAsync(identifier, DemoDataSeeder.DemoPassword));
        Assert.True(result.IsSuccess, result.Error.Message);
        var u = result.Value!;
        return new Actor(u.AccountId, u.Name, u.Role, u.AdminRole, u.StartupId, u.InvestorId, u.TeamAccountId, MfaSatisfied: u.Role == PrimaryRole.Admin);
    }

    public Task<Actor> Founder() => SignIn("farhan@nodifreight.example");         // Nodi Freight (verified, connected to Sadia)
    public Task<Actor> PendingFounder() => SignIn("ayesha@krishicloud.example");  // Krishi Cloud (pending verification)
    public Task<Actor> Investor() => SignIn("sadia@padmavc.example");             // Sadia Haque (verified)
    public Task<Actor> Imran() => SignIn("imran@angel.example");                  // verified, receives pitches
    public Task<Actor> Nusrat() => SignIn("nusrat@meghnacap.example");            // verified, Receive Pitches OFF
    public Task<Actor> Admin(string role) => SignIn($"{role}@riseup.example");

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
