using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RiseUp.DataAccess.Seed;
using RiseUp.DataAccess.Services;
using RiseUp.Framework.Time;

namespace RiseUp.DataAccess;

/// <summary>
/// Used only by the EF Core tools (Package Manager Console: Add-Migration / Update-Database / Remove-Migration,
/// or `dotnet ef`). Migrations always target SQL Server.
/// Connection string lookup order:
///   1. env var RISEUP_MIGRATIONS_CONNECTION
///   2. ConnectionStrings:RiseUp from the startup project's appsettings.json / appsettings.{Environment}.json / env vars
///   3. LocalDB default
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RiseUpDbContext>
{
    private const string LocalDb = "Server=(localdb)\\MSSQLLocalDB;Database=RiseUp;Trusted_Connection=True;TrustServerCertificate=True";

    public RiseUpDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = FindStartupProjectDirectory();

        var sources = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            sources.AddUserSecrets("RiseUp.Web.UI-local-development");
        var configuration = sources.AddEnvironmentVariables().Build();

        var connection = Environment.GetEnvironmentVariable("RISEUP_MIGRATIONS_CONNECTION")
            ?? configuration.GetConnectionString("RiseUp")
            ?? LocalDb;

        var options = new DbContextOptionsBuilder<RiseUpDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(RiseUpDbContext).Assembly.FullName));
        if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
        {
            if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select Development to install the fictional demo.");
            var media = new SeedOptions { DemoMediaPath = Path.GetFullPath(configuration["Seed:DemoMediaPath"] ?? "DemoMedia", basePath) };
            var files = new LocalPrivateFileStorage(Options.Create(new StorageOptions
            {
                RootPath = Path.GetFullPath(configuration["Storage:RootPath"] ?? "App_Data/private", basePath)
            }));
            Task Seed(DbContext context, CancellationToken ct) =>
                new DemoDataSeeder((RiseUpDbContext)context, new PasswordHasherAdapter(), new SystemClock(), files, Options.Create(media))
                    .SeedAsync(requireDemo: true, ct);
            options.UseSeeding((context, _) => Seed(context, CancellationToken.None).GetAwaiter().GetResult())
                .UseAsyncSeeding((context, _, ct) => Seed(context, ct));
        }
        return new RiseUpDbContext(options.Options, new SystemClock());
    }

    /// <summary>The EF tools run with the startup project as the working directory; fall back to src/RiseUp.Web.UI.</summary>
    private static string FindStartupProjectDirectory()
    {
        var current = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(current, "appsettings.json"))) return current;
        var dir = new DirectoryInfo(current);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "RiseUp.Web.UI");
            if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;
            candidate = Path.Combine(dir.FullName, "RiseUp.Web.UI");
            if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;
            dir = dir.Parent;
        }
        return current;
    }
}
