using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
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
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = FindStartupProjectDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = Environment.GetEnvironmentVariable("RISEUP_MIGRATIONS_CONNECTION")
            ?? configuration.GetConnectionString("RiseUp")
            ?? LocalDb;

        var options = new DbContextOptionsBuilder<RiseUpDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(RiseUpDbContext).Assembly.FullName))
            .Options;
        return new RiseUpDbContext(options, new SystemClock());
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
